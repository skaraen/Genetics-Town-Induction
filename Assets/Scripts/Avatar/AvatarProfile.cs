using UnityEngine;

[System.Serializable]
public class AvatarProfile
{
    const int NUM_SUBSEQUENCES = 2;
    public int id;
    public string name;
    public string baseSequence;
    public string[] subSequences;
    public string colorSequence;
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