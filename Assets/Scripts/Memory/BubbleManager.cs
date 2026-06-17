using System.Collections.Generic;
using UnityEngine;
using System;

public class BubbleManager : MonoBehaviour
{
    [Header("UI & Prefabs")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject spawnContainer;
    [SerializeField] private Scroller selfScroller;
    [SerializeField] private Scroller oppScroller;

    [Header("3D Preview")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RenderTexture defaultTexture;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Transform previewStage;
    [SerializeField] private GameObject defPrefab;

    [Header ("Mode")]
    [SerializeField] private bool isInfectionMode = false;

    private List<PlayerCustoms> playerCustoms;
    private GameObject currModel;
    private Bubble currBubble;
    private int currIdx;
    private string bug;
    int numRows;
    int maxStartRow;
    
    private Dictionary<string, string> sequenceLookup = new();

    void OnEnable()
    {
        playerCustoms = PlayerLogbook.Instance.GetAllCustoms();

        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            bug = spawner.GetBugSequence();
            Debug.Log($"[BubbleManager] The bug sequence is: {bug}");
        }

        if (isInfectionMode)
            SetRowParams(AvatarRegistry.Instance.GetCompleteSequence(playerCustoms[0]));

        for (int i = 0; i < playerCustoms.Count; i++)
        {
            SetupPlayerBubble(i);
        }
        currBubble = null;
        currIdx = -1;
    }

    void SetRowParams(string refSeq)
    {
        numRows = refSeq.Length / 3;
        maxStartRow = numRows - (bug.Length / 3);
    }

    string GetInfectedSequence(string seq)
    {
        if (string.IsNullOrEmpty(seq) || string.IsNullOrEmpty(bug) || seq.Length < bug.Length)
            return seq;
        
        int startRow = UnityEngine.Random.Range(0, maxStartRow + 1);
        int startIdx = startRow * 3;

        string result = seq.Remove(startIdx, bug.Length)
                        .Insert(startIdx, bug);

        Debug.Log($"Infected Sequence [{startIdx}, {bug}]: {result}");

        return result;
    }

    void SetupPlayerBubble(int idx)
    {
        PlayerCustoms customs = playerCustoms[idx];
        string actualSeq = AvatarRegistry.Instance.GetCompleteSequence(customs);
        string sequence = isInfectionMode ? GetInfectedSequence(actualSeq) : actualSeq;

        Debug.Log($"Used Sequence: {sequence}");

        if (idx == 0)
        {
            selfScroller.SetGeneSequence(sequence);
            return;
        }

        GameObject go = Instantiate(buttonPrefab, spawnContainer.transform);
        Bubble bubble = go.GetComponent<Bubble>();
        bubble.SetTitle(customs.name.ToString());
        bubble.SetColor(customs.color0);
        bubble.SetAvatarCustoms(customs);
        sequenceLookup.Add(customs.name.ToString(), sequence);

        bubble.InitializePreview(previewCamera, defaultTexture);
        bubble.button.onClick.AddListener(() => OnBubbleClicked(idx, bubble));
    }

    void OnBubbleClicked(int idx, Bubble bubble)
    {
        UIAudioManager.Instance?.PlayBubbleClick();

        if (currBubble != null)
            ResetBubble(currBubble, currIdx);
        currBubble = bubble;
        currIdx = idx;

        if (currModel) { 
            Destroy(currModel); 
            currModel = null; 
        }

        if (previewStage)
        {
            currModel = AvatarLoader.Load(
                bubble.customsToShow, 
                previewStage.position, 
                Quaternion.identity, 
                previewStage
            );

            if (currModel != null)
            {
                currModel.transform.localPosition = new Vector3(0f, bubble.GetAvatarYOffset(), 0f);
                currModel.transform.localScale = Vector3.one * 0.75f;
                currModel.transform.localRotation = Quaternion.identity;

                SetLayerRecursively(currModel, LayerMask.NameToLayer("BubblePreview"));
                previewCamera.transform.LookAt(previewStage.position);
            }
        }

        bubble.SetTexture(renderTexture);
        bubble.SetTitle("");
        oppScroller.SetScrollTitle(playerCustoms[idx].name.ToString());
        string sequence = sequenceLookup[playerCustoms[idx].name.ToString()];
        if (!String.IsNullOrEmpty(sequence))
            oppScroller.SetGeneSequence(sequence);
    }

    void ResetBubble(Bubble bubble, int idx) {
        bubble.SetTitle(playerCustoms[idx].name.ToString());
        bubble.SetColor(playerCustoms[idx].color0);
        bubble.SetTexture(defaultTexture);
    }

    public void HideCurrentModel()
    {
        if (currModel) { Destroy(currModel); currModel = null; }
    }

    static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
