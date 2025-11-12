using UnityEngine;
using System;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string emotion;
    public string text;
}

[System.Serializable]
public class DialogueSequence
{
    public string sequence;
    public string mode;
    public DialogueLine[] dialogues;
}

[System.Serializable]
public class DialogueCollection
{
    public DialogueSequence[] sequences;
}
