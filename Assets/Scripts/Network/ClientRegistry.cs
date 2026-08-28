using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ClientRegistry : MonoBehaviour
{
    private readonly Dictionary<PlayerRef, GameObject> clientPlayers = new();

    private void PopulateRegistry()
    {
        clientPlayers.Clear();
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player p in players)
        {
            NetworkObject networkObject = p.GetComponent<NetworkObject>();
            if (networkObject != null)
                clientPlayers[networkObject.InputAuthority] = p.gameObject;
        }

        Debug.Log($"Client list populated : {clientPlayers.Count}");
    }

    public GameObject FindPlayerByPlayerRef(PlayerRef player)
    {
        if (clientPlayers.Count == 0 || !clientPlayers.ContainsKey(player))
            PopulateRegistry();

        return clientPlayers.TryGetValue(player, out GameObject playerObject) ? playerObject : null;
    }

    public GameObject FindPlayerByClientId(ulong clientId)
    {
        if (clientPlayers.Count == 0)
            PopulateRegistry();

        foreach (var pair in clientPlayers)
        {
            if ((ulong)pair.Key.PlayerId == clientId)
                return pair.Value;
        }

        return null;
    }
}
