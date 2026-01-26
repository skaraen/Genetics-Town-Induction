using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientRegistry : MonoBehaviour
{
    private Dictionary<ulong, GameObject> clientPlayers;

    void Start()
    {
        clientPlayers = new Dictionary<ulong, GameObject>();
    }

    void PopulateRegistry()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player p in players)
        {
        ulong clientId = p.GetComponent<NetworkObject>().OwnerClientId;
            clientPlayers.Add(clientId, p.gameObject);
        }

        Debug.Log($"Client list populated : {clientPlayers.Count}");
    }

    public GameObject FindPlayerByClientId(ulong clientId)
    {
        if (clientPlayers.Count == 0)
            PopulateRegistry();

        return clientPlayers[clientId];
    }
}
