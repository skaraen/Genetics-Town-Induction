using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMetadataRegistry : NetworkBehaviour
{
    public static PlayerMetadataRegistry Instance { get; private set; }

    [SerializeField] private int interactionThreshold;
    [SerializeField] private List<string> bugSequences = new();
    private readonly Dictionary<ulong, PlayerCustoms> store = new();
    private readonly HashSet<ulong> ready = new();
    private HashSet<ulong> interacting = new();
    private int numPlayers = 0;
    private int numInteractions = 0;
    
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
            numPlayers = NetworkManager.ConnectedClients.Count;
            interactionThreshold = (numPlayers * (numPlayers - 1) + 3) / 4;
            sceneSwitched = true;
            NetworkManager.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    // -------- Optional: allow clients to request others' metadata later
    [ServerRpc(RequireOwnership = false)]
    public void RequestMetadataServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        if (store.TryGetValue(targetClientId, out var data))
        {
            SendMetadataClientRpc(targetClientId, data, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
            });
        }
        else
        {
            SendNotFoundClientRpc((int)targetClientId, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
            });
        }
    }

    [ClientRpc]
    private void SendMetadataClientRpc(ulong id, PlayerCustoms data, ClientRpcParams clientRpcParams = default)
    {
        PlayerLogbook.Instance.AddPlayerCustom(id, data);
    }

    [ClientRpc]
    private void SendNotFoundClientRpc(int targetId, ClientRpcParams clientRpcParams = default)
    {
        Debug.LogWarning($"[Registry] No metadata for id {targetId} yet.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterInteractionServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        numInteractions += 1;
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        interacting.Add(targetClientId);
        interacting.Add(senderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitInteractionServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        if (interacting.Contains(senderClientId))
            interacting.Remove(senderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InfectionReadinessServerRpc(ServerRpcParams rpcParams = default)
    {
        if (numInteractions >= interactionThreshold && interacting.Count == 0)
        {
            Debug.Log("[InfectionServerRpc] Initiate infection");
            var rand = new System.Random();
            int index = rand.Next(bugSequences.Count);
            string bugSeq = bugSequences[index];

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ClientRpcParams sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { senderClientId }
                }
            };
            StartInfectionClientRpc(bugSeq, sendParams);
        } else
            Debug.Log($"[InfectionServerRpc] Not ready for infection: {interacting.Count}");
    }

    [ClientRpc] 
    public void StartInfectionClientRpc(string bugSeq, ClientRpcParams rpcParams = default)
    {
        Debug.Log("[InfectionClientRpc] Use spawner to infect");

        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        spawner.StartInfection(bugSeq);
    }
}
