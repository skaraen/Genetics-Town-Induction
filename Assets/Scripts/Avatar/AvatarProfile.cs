using UnityEngine;

/// <summary>
/// Unity representation of data loaded from AvatarProfiles.json
/// This should really be its own ScriptableObject instead of loading data from JSON
/// </summary>
[System.Serializable]
public class AvatarProfile
{
    const int NUM_SUBSEQUENCES = 2;
    // each avatar profiel has a unique ID that cooresponds to their avatarType
    // e.g 'jellyfish' = 1
    public int id;
    public string name; // the name of the avatar - e.g 'jellyfish'
    public string baseSequence; // DNA base sequence, e.g 'AGTAA...'
    public string baseSequenceTraitName; // name of trait controlled by base sequence
    // Sub sequences of DNA, e.g ["AGCT", "CTGA"]
    // for now, each character has 2 
    public string[] subSequences; // 2 subsequences for distinct changeable traits

    // names of traits controlled by each subsequence
    // must be same length as subSequences
    public string[] subSequencesTraitNames;
    public string colorSequence; // the singluar color sequence for this avatar
    // Array of gameobject names that should be colored based on this avatar's genetics
    public string[] colorTargets;
    public string[] colorRanges;
    public string info;
    public string prefabPath;

    public AvatarProfile()
    {
        subSequences = new string[NUM_SUBSEQUENCES];
        subSequencesTraitNames = new string[NUM_SUBSEQUENCES];
        colorTargets = new string[2 * NUM_SUBSEQUENCES];
        colorRanges = new string[2 * NUM_SUBSEQUENCES];
    }
}