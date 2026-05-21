using System;
using UnityEngine;

[System.Serializable]
public class PlayerMetadata
{
    const int NUM_SUBSEQUENCES = 2;
    public int id;
    public int avatarType;

    public string name;
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

    public PlayerMetadata(AvatarProfileData profile) : this()
    {
        avatarType = profile.id;
        baseSequence = profile.baseSequence;
        Array.Copy(profile.subSequences, subSequences, NUM_SUBSEQUENCES);
    }

    public string GetCompleteSequence()
    {
        string subStr = "";
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
            subStr += sequenceEnabled[i] ? $"ATG{subSequences[i]}" : $"{subSequences[i]}TAG";
        return "ATG" + baseSequence + subStr;
    }
}