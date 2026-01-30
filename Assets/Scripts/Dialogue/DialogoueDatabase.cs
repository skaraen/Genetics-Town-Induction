using System.Collections.Generic;
using UnityEngine;

public class DialogueDatabase : MonoBehaviour
{
    public static DialogueDatabase Instance { get; private set; }

    [Header("JSON Source")]
    [SerializeField] private TextAsset dialogueJson;

    private Dictionary<string, DialogueSequence> byName;
    private DialogueCollection collection;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOnce();
    }

    private void LoadOnce()
    {
        if (dialogueJson == null)
        {
            Debug.LogError("[DialogueDatabase] No dialogue JSON assigned.");
            byName = new Dictionary<string, DialogueSequence>();
            return;
        }

        string raw = dialogueJson.text.Trim();

        if (raw.StartsWith("["))
            raw = "{ \"sequences\": " + raw + " }";

        collection = JsonUtility.FromJson<DialogueCollection>(raw);

        byName = new Dictionary<string, DialogueSequence>();
        if (collection?.sequences != null)
        {
            foreach (var seq in collection.sequences)
            {
                if (!string.IsNullOrEmpty(seq?.sequence))
                    byName[seq.sequence] = seq;
            }
        }
    }

    public bool HasSequence(string name) => byName != null && byName.ContainsKey(name);

    public DialogueSequence GetSequence(string name)
    {
        if (byName != null && byName.TryGetValue(name, out var seq))
            return seq;

        Debug.LogWarning($"[DialogueDatabase] Sequence not found: {name}");
        return null;
    }

    public DialogueLine[] GetLines(string name)
    {
        var seq = GetSequence(name);
        return seq?.dialogues;
    }

    public IEnumerable<string> AllSequenceNames()
    {
        if (byName == null) yield break;
        foreach (var k in byName.Keys) yield return k;
    }
}
