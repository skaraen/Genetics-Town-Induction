using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central registry of all player-selectable avatar prefabs.
/// Loaded from Resources as a lazy singleton ScriptableObject.
/// Replaces AvatarProfileManager by reading AvatarProfileData directly from prefab assets.
/// </summary>
[CreateAssetMenu(fileName = "AvatarRegistry", menuName = "Game/Avatar Registry")]
public class AvatarRegistry : ScriptableObject
{
    [Tooltip("All avatar prefabs, each with an AvatarProfileData component on the root")]
    [SerializeField] private GameObject[] avatarPrefabs;

    private static AvatarRegistry _instance;
    public static AvatarRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AvatarRegistry>("Avatars/AvatarRegistry");
                if (_instance == null)
                    Debug.LogError("AvatarRegistry asset not found at Resources/Avatars/AvatarRegistry");
                else
                    _instance.BuildLookups();
            }
            return _instance;
        }
    }

    private Dictionary<int, GameObject> idToPrefab;
    private Dictionary<string, GameObject> sequenceToPrefab;

    /// <summary>
    /// Builds internal lookup dictionaries from AvatarProfileData on each prefab.
    /// Called once on first access.
    /// </summary>
    private void BuildLookups()
    {
        idToPrefab = new Dictionary<int, GameObject>();
        sequenceToPrefab = new Dictionary<string, GameObject>();

        foreach (var prefab in avatarPrefabs)
        {
            var profile = prefab.GetComponent<AvatarProfileData>();
            if (profile == null)
            {
                Debug.LogError($"Avatar prefab '{prefab.name}' is missing AvatarProfileData component");
                continue;
            }
            idToPrefab[profile.id] = prefab;
            if (!string.IsNullOrEmpty(profile.baseSequence))
                sequenceToPrefab[profile.baseSequence] = prefab;
        }
    }

    /// <summary>
    /// Returns the prefab GameObject for the given avatar type ID.
    /// </summary>
    public GameObject GetPrefabById(int id)
    {
        return idToPrefab.TryGetValue(id, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Returns the AvatarProfileData from the prefab asset for the given ID.
    /// Safe for reading string/int fields. For Renderer[] access, use
    /// GetComponent on the instantiated clone instead.
    /// </summary>
    public AvatarProfileData GetProfileById(int id)
    {
        var prefab = GetPrefabById(id);
        return prefab != null ? prefab.GetComponent<AvatarProfileData>() : null;
    }

    /// <summary>
    /// Returns the prefab whose AvatarProfileData.baseSequence matches.
    /// </summary>
    public GameObject GetPrefabBySequence(string baseSequence)
    {
        return sequenceToPrefab.TryGetValue(baseSequence, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Returns the AvatarProfileData for a base sequence string.
    /// </summary>
    public AvatarProfileData GetProfileBySequence(string baseSequence)
    {
        var prefab = GetPrefabBySequence(baseSequence);
        return prefab != null ? prefab.GetComponent<AvatarProfileData>() : null;
    }

    /// <summary>
    /// Returns all registered avatar profiles, useful for building
    /// dynamic UI elements like the avatar selection buttons.
    /// </summary>
    public AvatarProfileData[] GetAllProfiles()
    {
        return avatarPrefabs
            .Select(p => p.GetComponent<AvatarProfileData>())
            .Where(p => p != null)
            .ToArray();
    }

    /// <summary>
    /// Returns the full constructed sequence based on the player
    /// customs according to the rules of genetic biology 
    /// </summary>
    public string GetCompleteSequence(PlayerCustoms customs)
    {
        AvatarProfileData profileData = GetProfileById(customs.avatarType);
        string fullSeq = "";
        
        fullSeq += "ATG" + profileData.baseSequence;
        fullSeq += customs.subSeq0Enabled ? $"ATG{profileData.subSequences[0]}" : $"{profileData.subSequences[0]}TAG";
        fullSeq += customs.subSeq1Enabled ? $"ATG{profileData.subSequences[1]}" : $"{profileData.subSequences[1]}TAG";

        return fullSeq;
    }
}
