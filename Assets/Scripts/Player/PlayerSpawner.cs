using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : SimulationBehaviour, INetworkRunnerCallbacks
{
    [Header("Root player prefab")]
    [SerializeField] private GameObject rootPrefab;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Spawn locations")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float interval = 30f;

    public event Action OnSpawnComplete;
    public event Action OnInfected;
    public event Action OnCured;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();
    private readonly Dictionary<PlayerRef, int> spawnIndices = new();
    private bool isInfected;
    private string bug;
    private float timer;
    private NetworkRunner registeredRunner;

    private void Start()
    {
        RegisterCallbacks();

        if (PlayerMetadataRegistry.Instance != null)
            PlayerMetadataRegistry.Instance.OnSubmitted += OnMetadataSubmitted;

        TrySpawnExistingPlayers();
    }

    private void OnDestroy()
    {
        if (PlayerMetadataRegistry.Instance != null)
            PlayerMetadataRegistry.Instance.OnSubmitted -= OnMetadataSubmitted;

        if (registeredRunner != null)
        {
            registeredRunner.RemoveGlobal(this);
            registeredRunner.RemoveCallbacks(this);
        }
    }

    private void Update()
    {
        if (registeredRunner == null)
            RegisterCallbacks();

        NetworkRunner runner = GetRunner();
        if (runner == null || !runner.IsServer || isInfected)
            return;

        timer += Time.deltaTime;
        if (timer < interval)
            return;

        timer = 0f;
        PlayerMetadataRegistry.Instance?.InfectionReadinessServerRpc();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        TrySpawnFor(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !runner.IsServer)
            return;

        PlayerMetadataRegistry.Instance?.Remove(player);

        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject) && playerObject != null)
            runner.Despawn(playerObject);

        spawnedPlayers.Remove(player);
        spawnIndices.Remove(player);
    }

    private void TrySpawnExistingPlayers()
    {
        NetworkRunner runner = GetRunner();
        if (runner == null || !runner.IsServer)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
            TrySpawnFor(player);
    }

    private void OnMetadataSubmitted(PlayerRef player, PlayerCustoms _)
    {
        if (SceneManager.GetActiveScene().name == gameSceneName)
            TrySpawnFor(player);
    }

    private void TrySpawnFor(PlayerRef player)
    {
        NetworkRunner runner = GetRunner();
        if (runner == null || !runner.IsServer)
            return;

        if (SceneManager.GetActiveScene().name != gameSceneName || spawnedPlayers.ContainsKey(player))
            return;

        if (PlayerMetadataRegistry.Instance == null ||
            !PlayerMetadataRegistry.Instance.TryGetCustoms(player, out PlayerCustoms customs))
        {
            Debug.Log($"[Spawner] Waiting for metadata for player {player}...");
            return;
        }

        NetworkObject networkPrefab = rootPrefab != null ? rootPrefab.GetComponent<NetworkObject>() : null;
        if (networkPrefab == null)
        {
            Debug.LogError("[Spawner] Root prefab must have a Photon Fusion NetworkObject component.");
            return;
        }

        var (position, rotation) = GetSpawnFor(player);
        NetworkObject playerObject = runner.Spawn(networkPrefab, position, rotation, player);
        spawnedPlayers[player] = playerObject;
        runner.SetPlayerObject(player, playerObject);

        if (playerObject.TryGetComponent(out PlayerVisual visual))
            visual.SetAvatarCustoms(customs);

        Debug.Log($"[Spawner] Spawned player for {player} (avatarType={customs.avatarType}).");
        OnSpawnComplete?.Invoke();
        RPC_NotifySpawnComplete(runner);
    }

    private (Vector3, Quaternion) GetSpawnFor(PlayerRef player)
    {
        int index = GetSpawnIndex(player);
        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
            return (spawnPoints[index].position, spawnPoints[index].rotation);

        return (Vector3.zero, Quaternion.identity);
    }

    public void StartInfection(string seq)
    {
        StartInfection(seq, PlayerRef.None);
    }

    public void StartInfection(string seq, PlayerRef targetPlayer)
    {
        NetworkRunner runner = GetRunner();
        if (runner == null || !runner.IsServer)
            return;

        isInfected = true;
        bug = seq;

        if (targetPlayer != PlayerRef.None && spawnedPlayers.TryGetValue(targetPlayer, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent(out PlayerVisual visual))
                visual.SetInfected(true);
        }
        else
        {
            foreach (NetworkObject playerObject in spawnedPlayers.Values)
            {
                if (playerObject != null && playerObject.TryGetComponent(out PlayerVisual visual))
                    visual.SetInfected(true);
            }
        }

        RPC_NotifyInfected(runner, seq);
    }

    public string GetBugSequence()
    {
        return bug;
    }

    public void CureInfectionServerRpc()
    {
        NetworkRunner runner = GetRunner();
        if (runner != null)
            RPC_RequestCure(runner);
    }

    private void CureFor(NetworkRunner runner, PlayerRef player)
    {
        if (!spawnedPlayers.TryGetValue(player, out NetworkObject playerObject) || playerObject == null)
            return;

        if (playerObject.TryGetComponent(out PlayerVisual visual))
            visual.SetInfected(false);

        RPC_NotifyCured(runner, player);
    }

    private int GetSpawnIndex(PlayerRef player)
    {
        if (spawnIndices.TryGetValue(player, out int existingIndex))
            return existingIndex;

        int nextIndex = spawnIndices.Count;
        if (spawnPoints != null && spawnPoints.Length > 0)
            nextIndex %= spawnPoints.Length;

        spawnIndices[player] = nextIndex;
        return nextIndex;
    }

    private NetworkRunner GetRunner()
    {
        return Runner != null ? Runner : registeredRunner != null ? registeredRunner : FindFirstObjectByType<NetworkRunner>();
    }

    private void RegisterCallbacks()
    {
        NetworkRunner runner = GetRunner();

        if (runner == null || !runner.IsRunning || registeredRunner == runner)
            return;

        if (registeredRunner != null)
        {
            registeredRunner.RemoveGlobal(this);
            registeredRunner.RemoveCallbacks(this);
        }

        registeredRunner = runner;
        registeredRunner.AddGlobal(this);
        registeredRunner.AddCallbacks(this);
    }

    private static PlayerRef ResolveSource(NetworkRunner runner, RpcInfo info)
    {
        return info.Source == PlayerRef.None ? runner.LocalPlayer : info.Source;
    }

    [Rpc]
    private static void RPC_NotifySpawnComplete(NetworkRunner runner)
    {
        FindFirstObjectByType<PlayerSpawner>()?.OnSpawnComplete?.Invoke();
    }

    [Rpc]
    private static void RPC_NotifyInfected(NetworkRunner runner, string seq)
    {
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
            return;

        spawner.bug = seq;
        spawner.OnInfected?.Invoke();
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_RequestCure(NetworkRunner runner, RpcInfo info = default)
    {
        if (!runner.IsServer)
            return;

        FindFirstObjectByType<PlayerSpawner>()?.CureFor(runner, ResolveSource(runner, info));
    }

    [Rpc]
    private static void RPC_NotifyCured(NetworkRunner runner, [RpcTarget] PlayerRef target)
    {
        FindFirstObjectByType<PlayerSpawner>()?.OnCured?.Invoke();
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        TrySpawnExistingPlayers();
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
