// PlayerSpawner.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
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

    private bool isInfected = false;
    private string bug;
    private float timer = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { enabled = false; return; }

        NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;

        if (PlayerMetadataRegistry.Instance != null)
            PlayerMetadataRegistry.Instance.OnSubmitted += OnMetadataSubmitted;
    }

    private void OnDestroy()
    {
        if (NetworkManager != null)
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;

        if (PlayerMetadataRegistry.Instance != null)
            PlayerMetadataRegistry.Instance.OnSubmitted -= OnMetadataSubmitted;
    }

    private void Update()
    {
        if (!IsServer || isInfected) return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            Debug.Log("[PlayerSpawner] Checking for infection");
            timer = 0f;
            PlayerMetadataRegistry.Instance.InfectionReadinessServerRpc();
        }
    }

    private void OnSceneEvent(SceneEvent evt)
    {
        if (!IsServer || evt.SceneName != gameSceneName) return;

        if (evt.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
                TrySpawnFor(clientId);
        }
        else if (evt.SceneEventType == SceneEventType.LoadComplete)
        {
            TrySpawnFor(evt.ClientId);
        }
    }

    private void OnMetadataSubmitted(ulong clientId, PlayerCustoms _)
    {
        if (!IsServer) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != gameSceneName) return;
        if (NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId) != null) return;

        TrySpawnFor(clientId);
    }

    private void TrySpawnFor(ulong clientId)
    {
        if (NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId) != null) return;

        if (PlayerMetadataRegistry.Instance == null ||
            !PlayerMetadataRegistry.Instance.TryGetCustoms(clientId, out var customs))
        {
            Debug.Log($"[Spawner] Waiting for metadata for client {clientId}...");
            return;
        }

        var (pos, rot) = GetSpawnFor(clientId);
        var rootObject = Instantiate(rootPrefab, pos, rot);
        rootObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);

        rootObject.GetComponent<PlayerVisual>().AvatarCustoms.Value = customs;

        Debug.Log($"[Spawner] Spawned player for {clientId} (avatarType={customs.avatarType}).");
        OnSpawnComplete?.Invoke();
        NotifySpawnCompleteClientRpc();
    }

    private (Vector3, Quaternion) GetSpawnFor(ulong clientId)
    {
        if (clientId < 4)
            return (spawnPoints[clientId].position, spawnPoints[clientId].rotation);
        return (Vector3.zero, Quaternion.identity);
    }

    public void StartInfection(string seq)
    {
        if (!IsServer) return;

        isInfected = true;
        Debug.Log("[PlayerSpawner] Start infection");

        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            var playerObj = client.PlayerObject;
            if (playerObj == null)
                continue;

            if (playerObj.TryGetComponent<PlayerVisual>(out var visual))
            {
                visual.IsInfected.Value = true;
            }
        }
        
        bug = seq;
        OnInfected?.Invoke();
        NotifyInfectedClientRpc(seq);
    }

    public string GetBugSequence()
    {
        return bug;
    }

    /// <summary>
    /// Broadcasts spawn complete notification to all clients.
    /// Called by server after a player is spawned.
    /// </summary>
    [ClientRpc]
    private void NotifySpawnCompleteClientRpc()
    {
        if (IsHost) return;
        OnSpawnComplete?.Invoke();
    }

    /// <summary>
    /// Broadcasts infection notification to all clients.
    /// Called by server after infection starts.
    /// </summary>
    [ClientRpc]
    private void NotifyInfectedClientRpc(string seq)
    {
        if (IsHost) return;

        bug = seq;
        OnInfected?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CureInfectionServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var client))
            return;

        var playerObj = client.PlayerObject;
        if (playerObj == null)
            return;

        if (playerObj.TryGetComponent<PlayerVisual>(out var visual))
        {
            visual.IsInfected.Value = false;
        }
    }
}
