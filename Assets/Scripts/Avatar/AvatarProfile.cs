using UnityEngine;

[System.Serializable]
public class AvatarProfile
{
    const int NUM_SUBSEQUENCES = 2;
    // each avatar profiel has a unique ID that cooresponds to their avatarType
    // e.g 'jellyfish' = 1
    public int id;
    public string name; // the name of the avatar - e.g 'jellyfish'
    public string baseSequence; // DNA base sequence, e.g 'AGTAA...'
    // Sub sequences of DNA, e.g ["AGCT", "CTGA"]
    // for now, each character has 2 
    public string[] subSequences;
    public string colorSequence; // the singluar color sequence for this avatar
    // Array of gameobject names that should be colored based on this avatar's genetics
    public string[] colorTargets;
    public string[] colorRanges;
    public string info;
    public string prefabPath;

    public AvatarProfile()
    {
        subSequences = new string[NUM_SUBSEQUENCES];
        colorTargets = new string[2 * NUM_SUBSEQUENCES];
        colorRanges = new string[2 * NUM_SUBSEQUENCES];
    }
}