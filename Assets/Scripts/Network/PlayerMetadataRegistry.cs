using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMetadataRegistry : NetworkBehaviour
{
    public static PlayerMetadataRegistry Instance { get; private set; }

    private readonly Dictionary<ulong, PlayerCustoms> store = new();
    private readonly HashSet<ulong> ready = new();
    private bool sceneSwitched;

    public event Action<ulong, PlayerCustoms> OnSubmitted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        store.Remove(clientId);
        ready.Remove(clientId);
    }

    public bool TryGetCustoms(ulong clientId, out PlayerCustoms data)
    {
        if (!IsServer) { data = default; return false; }
        return store.TryGetValue(clientId, out data);
    }

    public bool AllPlayersReady()
    {
        var ids = NetworkManager.ConnectedClientsIds;
        if (ids.Count == 0) return false;
        foreach (var id in ids) if (!ready.Contains(id)) return false;
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitMetadataServerRpc(PlayerCustoms data, ServerRpcParams rpcParams = default)
    {
        var sender = rpcParams.Receive.SenderClientId;
        store[sender] = data;
        ready.Add(sender);

        OnSubmitted?.Invoke(sender, data);
        TryStartGameIfReady();
    }

    private void TryStartGameIfReady()
    {
        if (!IsServer || sceneSwitched) return;
        if (AllPlayersReady())
        {
            sceneSwitched = true;
            NetworkManager.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    // -------- Optional: allow clients to request others' metadata later
    [ServerRpc(RequireOwnership = false)]
    public void RequestMetadataServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        var requester = rpcParams.Receive.SenderClientId;

        if (store.TryGetValue(targetClientId, out var data))
        {
            SendMetadataClientRpc((int)targetClientId, data, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { requester } }
            });
        }
        else
        {
            SendNotFoundClientRpc((int)targetClientId, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { requester } }
            });
        }
    }

    [ClientRpc]
    private void SendMetadataClientRpc(int id, PlayerCustoms data, ClientRpcParams clientRpcParams = default)
    {
        var local = MetaDataCustomsMapper.ToLocal(id, data);
        PlayerMetadataManager.Instance.AddPlayerMetadata(local);
    }

    [ClientRpc]
    private void SendNotFoundClientRpc(int targetId, ClientRpcParams clientRpcParams = default)
    {
        Debug.LogWarning($"[Registry] No metadata for id {targetId} yet.");
    }
}
