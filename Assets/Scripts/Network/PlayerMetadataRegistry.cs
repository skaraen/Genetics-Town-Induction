using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMetadataRegistry : SimulationBehaviour
{
    public static PlayerMetadataRegistry Instance { get; private set; }

    [SerializeField] private int interactionThreshold;
    [SerializeField] private List<string> bugSequences = new();

    private readonly Dictionary<PlayerRef, PlayerCustoms> store = new();
    private readonly HashSet<PlayerRef> ready = new();
    private readonly HashSet<PlayerRef> interacting = new();
    private int numInteractions;
    private bool sceneSwitched;

    public event Action<PlayerRef, PlayerCustoms> OnSubmitted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SubmitMetadata(PlayerCustoms data)
    {
        NetworkRunner runner = GetRunner();
        if (runner == null)
        {
            Debug.LogError("[Registry] Cannot submit metadata before Fusion runner exists.");
            return;
        }

        RPC_SubmitMetadata(runner, data);
    }

    public bool TryGetCustoms(PlayerRef player, out PlayerCustoms data)
    {
        return store.TryGetValue(player, out data);
    }

    public bool TryGetCustoms(ulong playerId, out PlayerCustoms data)
    {
        foreach (var pair in store)
        {
            if ((ulong)pair.Key.PlayerId == playerId)
            {
                data = pair.Value;
                return true;
            }
        }

        data = default;
        return false;
    }

    public void Remove(PlayerRef player)
    {
        store.Remove(player);
        ready.Remove(player);
        interacting.Remove(player);
    }

    public bool AllPlayersReady()
    {
        NetworkRunner runner = GetRunner();
        if (runner == null)
            return false;

        int playerCount = 0;
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            playerCount++;
            if (!ready.Contains(player))
                return false;
        }

        return playerCount > 0;
    }

    public void RequestMetadata(PlayerRef targetPlayer)
    {
        NetworkRunner runner = GetRunner();
        if (runner != null)
            RPC_RequestMetadata(runner, targetPlayer);
    }

    public void RegisterInteractionServerRpc(PlayerRef targetPlayer)
    {
        NetworkRunner runner = GetRunner();
        if (runner != null)
            RPC_RegisterInteraction(runner, targetPlayer);
    }

    public void ExitInteractionServerRpc()
    {
        NetworkRunner runner = GetRunner();
        if (runner != null)
            RPC_ExitInteraction(runner);
    }

    public void InfectionReadinessServerRpc()
    {
        NetworkRunner runner = GetRunner();
        if (runner == null)
            return;

        if (runner.IsServer)
            TryStartInfectionFor(runner.LocalPlayer);
        else
            RPC_CheckInfectionReadiness(runner);
    }

    private void StoreSubmitted(NetworkRunner runner, PlayerRef player, PlayerCustoms data)
    {
        store[player] = data;
        ready.Add(player);
        OnSubmitted?.Invoke(player, data);
        TryStartGameIfReady(runner);
    }

    private void TryStartGameIfReady(NetworkRunner runner)
    {
        if (!runner.IsServer || sceneSwitched || !AllPlayersReady())
            return;

        int playerCount = 0;
        foreach (PlayerRef _ in runner.ActivePlayers)
            playerCount++;

        interactionThreshold = (playerCount * (playerCount - 1) + 3) / 4;
        sceneSwitched = true;
        if (runner.IsSceneAuthority)
            runner.LoadScene("GameScene", LoadSceneMode.Single);
    }

    private void TryStartInfectionFor(PlayerRef targetPlayer)
    {
        if (numInteractions < interactionThreshold || interacting.Count != 0)
        {
            Debug.Log($"[Registry] Not ready for infection: {interacting.Count}");
            return;
        }

        if (bugSequences.Count == 0)
        {
            Debug.LogWarning("[Registry] No bug sequences configured.");
            return;
        }

        string bugSeq = bugSequences[UnityEngine.Random.Range(0, bugSequences.Count)];
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        spawner?.StartInfection(bugSeq, targetPlayer);
    }

    private static PlayerRef ResolveSource(NetworkRunner runner, RpcInfo info)
    {
        return info.Source == PlayerRef.None ? runner.LocalPlayer : info.Source;
    }

    private static NetworkRunner GetRunner()
    {
        return FindFirstObjectByType<NetworkRunner>();
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_SubmitMetadata(NetworkRunner runner, PlayerCustoms data, RpcInfo info = default)
    {
        if (!runner.IsServer)
            return;

        PlayerRef sender = ResolveSource(runner, info);
        Instance?.StoreSubmitted(runner, sender, data);
        RPC_CacheMetadata(runner, sender, data);
    }

    [Rpc]
    private static void RPC_CacheMetadata(NetworkRunner runner, PlayerRef player, PlayerCustoms data)
    {
        if (Instance == null)
            return;

        Instance.store[player] = data;
        PlayerLogbook.Instance?.AddPlayerCustom((ulong)player.PlayerId, data);
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_RequestMetadata(NetworkRunner runner, PlayerRef targetPlayer, RpcInfo info = default)
    {
        PlayerRef sender = ResolveSource(runner, info);
        if (Instance != null && Instance.store.TryGetValue(targetPlayer, out PlayerCustoms data))
            RPC_SendMetadata(runner, sender, targetPlayer, data);
        else
            RPC_SendNotFound(runner, sender, targetPlayer.PlayerId);
    }

    [Rpc]
    private static void RPC_SendMetadata(NetworkRunner runner, [RpcTarget] PlayerRef target, PlayerRef player, PlayerCustoms data)
    {
        PlayerLogbook.Instance?.AddPlayerCustom((ulong)player.PlayerId, data);
    }

    [Rpc]
    private static void RPC_SendNotFound(NetworkRunner runner, [RpcTarget] PlayerRef target, int targetId)
    {
        Debug.LogWarning($"[Registry] No metadata for id {targetId} yet.");
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_RegisterInteraction(NetworkRunner runner, PlayerRef targetPlayer, RpcInfo info = default)
    {
        if (Instance == null || !runner.IsServer)
            return;

        PlayerRef sender = ResolveSource(runner, info);
        Instance.numInteractions += 1;
        Instance.interacting.Add(targetPlayer);
        Instance.interacting.Add(sender);
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_ExitInteraction(NetworkRunner runner, RpcInfo info = default)
    {
        if (Instance == null || !runner.IsServer)
            return;

        Instance.interacting.Remove(ResolveSource(runner, info));
    }

    [Rpc(HostMode = RpcHostMode.SourceIsHostPlayer)]
    private static void RPC_CheckInfectionReadiness(NetworkRunner runner, RpcInfo info = default)
    {
        if (!runner.IsServer || Instance == null)
            return;

        Instance.TryStartInfectionFor(ResolveSource(runner, info));
    }
}
