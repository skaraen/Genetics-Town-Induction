using UnityEngine;

[System.Serializable]
public class PlayerMetadata
{
    const int NUM_SUBSEQUENCES = 2;
    public int id; // the player's randomly generated unique ID
    public int avatarType;
    public string name; // the player's custom online name - different from their avatar name
    public string baseSequence;
    public string[] subSequences;
    public bool[] sequenceEnabled;
    public Color[] colors;

    public PlayerMetadata()
    {
        subSequences = new string[NUM_SUBSEQUENCES];
        colors = new Color[NUM_SUBSEQUENCES];
        sequenceEnabled = new bool[NUM_SUBSEQUENCES];
    }

    public PlayerMetadata(AvatarProfileData profile)
    {
        subSequences = new string[NUM_SUBSEQUENCES];
        colors = new Color[NUM_SUBSEQUENCES];
        sequenceEnabled = new bool[NUM_SUBSEQUENCES];

        avatarType = profile.id;
        baseSequence = profile.baseSequence;
    }

    public string GetCompleteSequence()
    {
        string subStr = "";
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
            subStr += subSequences[i];
        return "ATG" + baseSequence + subStr;
    }
}