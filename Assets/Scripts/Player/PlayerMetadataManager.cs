using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerMetadataManager
{
    private static PlayerMetadataManager _instance;
    public static PlayerMetadataManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new PlayerMetadataManager();
            return _instance;
        }
    }

    private List<PlayerMetadata> playerData;
    private Dictionary<int, PlayerMetadata> idLookup;

    private PlayerMetadataManager()
    {
        playerData = new List<PlayerMetadata>();
        idLookup = new Dictionary<int, PlayerMetadata>();
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        Debug.Log("Load player metadata");
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/PlayerDummyData");
        if (jsonFile == null)
        {
            Debug.LogError("PlayerDummyData.json not found in Resources folder.");
            playerData = new List<PlayerMetadata>();
            idLookup = new Dictionary<int, PlayerMetadata>();
            return;
        }

        playerData = JsonUtilityWrapper.FromJsonArray<PlayerMetadata>(jsonFile.text);
        idLookup = playerData.ToDictionary(p => p.id);
    }

    public PlayerMetadata GetPlayerById(int id)
    {
        return idLookup.TryGetValue(id, out var player) ? player : null;
    }

    public List<PlayerMetadata> GetAllPlayers()
    {
        return playerData;
    }

    public List<PlayerMetadata> GetPlayersByAvatarType(int avatarType)
    {
        return playerData.Where(p => p.avatarType == avatarType).ToList();
    }

    public void AddPlayerMetadata(PlayerMetadata metadata)
    {
        int newId;
        do
        {
            newId = Random.Range(1, 100);
        } while (idLookup.ContainsKey(newId));

        metadata.id = newId;
        playerData.Add(metadata);
        idLookup.Add(metadata.id, metadata);
    }
}
