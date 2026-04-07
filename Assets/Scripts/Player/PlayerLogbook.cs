using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerLogbook : MonoBehaviour
{
    public static PlayerLogbook Instance { get; private set; }

    private List<PlayerCustoms> playerData = new();
    private Dictionary<ulong, PlayerCustoms> idLookup = new();
    private Dictionary<int, AvatarProfileData> profileLookup = new();

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

    public List<PlayerCustoms> GetAllCustoms() 
    { 
        return playerData; 
    } 
    
    public List<PlayerCustoms> GetCustomsByAvatarType(int avatarType) 
    { 
        return playerData.Where(p => p.avatarType == avatarType).ToList();
    }

    public PlayerCustoms? GetCustomsById(ulong id)
    {
        return idLookup.TryGetValue(id, out var player) ? player : null;
    }

    public void AddPlayerCustom(ulong id, PlayerCustoms customs)
    {
        idLookup[id] = customs;
        playerData.Add(customs);
    }
}