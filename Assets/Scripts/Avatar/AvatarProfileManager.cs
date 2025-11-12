using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AvatarProfileManager
{
    private static AvatarProfileManager _instance;
    public static AvatarProfileManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AvatarProfileManager();
            return _instance;
        }
    }

    private List<AvatarProfile> profiles;
    private Dictionary<string, AvatarProfile> sequenceLookup;

    private AvatarProfileManager()
    {
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Debug.Log("Load profiles");
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/AvatarProfiles");
        if (jsonFile == null)
        {
            Debug.LogError("AvatarProfiles.json not found in Resources folder.");
            profiles = new List<AvatarProfile>();
            sequenceLookup = new Dictionary<string, AvatarProfile>();
            return;
        }

        profiles = JsonUtilityWrapper.FromJsonArray<AvatarProfile>(jsonFile.text);
        sequenceLookup = profiles.ToDictionary(p => p.baseSequence);
    }

    public AvatarProfile GetProfileById(int id)
    {
        return profiles.FirstOrDefault(p => p.id == id);
    }

    public AvatarProfile GetProfileBySequence(string baseSequence)
    {
        return sequenceLookup.TryGetValue(baseSequence, out var profile) ? profile : null;
    }

    public string GetPathById(int id)
    {
        return profiles.FirstOrDefault(p => p.id == id).prefabPath;
    }

    public string[] GetColorTargetsById(int id)
    {
        AvatarProfile profile = profiles.FirstOrDefault(p => p.id == id);
        string[] targets = (string[])profile.colorTargets.Clone();
        return targets;
    }
}