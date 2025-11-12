using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class BubbleManager : MonoBehaviour
{
    [Header("UI & Prefabs")]
    public GameObject buttonPrefab;
    public GameObject spawnContainer;
    public GameObject selfContent;
    public GameObject oppContent;
    public TextMeshProUGUI oppTitle;

    [Header("3D Preview")]
    public Camera previewCamera;
    public RenderTexture defaultTexture;
    public RenderTexture renderTexture;
    public Transform previewStage;
    public GameObject defPrefab;

    private List<PlayerMetadata> metadata;
    private GameObject currModel;
    private Bubble currBubble;
    private int currIdx;

    void OnEnable()
    {
        metadata = PlayerMetadataManager.Instance.GetAllPlayers();
        Debug.Log($"Metadata size: {metadata.Count}");
        for (int i = 0; i < metadata.Count; i++)
        {
            SetupMetadata(i);
        }
        currBubble = null;
        currIdx = -1;
    }

    void SetupMetadata(int idx)
    {
        if (idx == 0)
        {
            SetScrollText(selfContent, metadata[idx].GetCompleteSequence());
            return;
        }
        GameObject go = Instantiate(buttonPrefab, spawnContainer.transform);
        Bubble bubble = go.GetComponent<Bubble>();
        bubble.SetTitle(metadata[idx].name);
        bubble.SetColor(metadata[idx].colors[0]);
        bubble.InitializePreview(previewCamera, defaultTexture, defPrefab);

        bubble.button.onClick.AddListener(() => OnBubbleClicked(idx, bubble));
    }

    void OnBubbleClicked(int idx, Bubble bubble)
    {
        if (currBubble != null) {
            Debug.Log("Reset bubble");
            ResetBubble(currBubble, currIdx);
        }
        currBubble = bubble;
        currIdx = idx;

        if (currModel) { 
            Destroy(currModel); 
            currModel = null; 
        }

        if (bubble.prefabToShow && previewStage)
        {
            currModel = Instantiate(bubble.prefabToShow, previewStage);
            currModel.transform.localPosition = Vector3.zero;
            currModel.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(currModel, LayerMask.NameToLayer("BubblePreview"));
            previewCamera.transform.LookAt(previewStage.position);
        }

        bubble.SetTexture(renderTexture);
        bubble.SetTitle("");
        SetScrollTitle(metadata[idx].name);
        SetScrollText(oppContent, metadata[idx].GetCompleteSequence());
    }

    void ResetBubble(Bubble bubble, int idx) {
        bubble.SetTitle(metadata[idx].name);
        bubble.SetColor(metadata[idx].colors[0]);
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

    void SetScrollText(GameObject content, string text)
        => content.GetComponent<TextMeshProUGUI>().text = text;

    void SetScrollTitle(string name)
    {
        string str = name.EndsWith("s") ? $"{name}' Code" : $"{name}'s Code";
        oppTitle.text = str;
    }
}
