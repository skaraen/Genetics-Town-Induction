using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using System;

public class AvatarGenerator : NetworkBehaviour
{
    const int NUM_SUBSEQUENCES = 2;
    const int NUM_CHOICES = 2;

    [Header("Avatar Spawn")]
    [SerializeField] Transform avatarSpawnPoint;
    [SerializeField] CameraRig cameraRig;
    [SerializeField] float xCamOffset = 3;
    [SerializeField] float yCamOffset = 1.5f;
    [SerializeField] float zCamOffset = 6;

    [Header("UI References")]
    [SerializeField] Button[] sequenceButtons;
    [SerializeField] Button nextButton;
    [SerializeField] GameObject[] canvas;
    [SerializeField] GameObject[] tickerItemPrefabs;
    [SerializeField] TextMeshProUGUI[] seqTitles;
    [SerializeField] Transform[] toggleTransforms;
    [SerializeField] Slider[] sliders;
    [SerializeField] Image[] dials;
    [SerializeField] Image[] needles;

    [SerializeField] TextMeshProUGUI titleTmp;
    [SerializeField] string[] titles;
    [SerializeField] GameObject subtitleObj;
    [SerializeField] TextMeshProUGUI subtitleTmp;
    [SerializeField] string[] subtitles;
    [SerializeField] Button zoomOutButton;
    [SerializeField] Button funFactButton;
    [SerializeField] GameObject funFactUI;

    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] TextMeshProUGUI waitMessage;


    const string set = "ATGC";
    string[] partNames = { "head", "torso" };
    private string[][] baseSequenceCodons;
    private Dictionary<string, GameObject> prefabMap;

    private string currBaseSequence;
    private AvatarProfileData currProfileData;
    private GameObject currentAvatar;
    private Color[] defaultColor;
    private Color[] currColor;
    private bool[] subSequenceEnabled;
    private SwitchToggle[] switchToggles;
    private Material[,] targetMaterials;
    private string playerName;

    private int currentStage = 0;

    void Start()
    {
        defaultColor = new Color[NUM_SUBSEQUENCES];
        currColor = new Color[NUM_SUBSEQUENCES];
        subSequenceEnabled = new bool[NUM_SUBSEQUENCES];
        switchToggles = new SwitchToggle[NUM_SUBSEQUENCES];
        targetMaterials = new Material[NUM_SUBSEQUENCES, NUM_CHOICES];

        BuildBaseSequenceCodons();
        ArrayUtils.ShuffleRows(baseSequenceCodons);

        canvas[0].SetActive(true);
        titleTmp.text = titles[0];
        subtitleTmp.text = subtitles[0];
        for (int i = 1; i < canvas.Length; i++)
        {
            canvas[i].SetActive(false);
        }
        SetupButtons();
        SetupToggles();
    }
    void OnEnable()
    {
        prefabMap = new Dictionary<string, GameObject>();
        LoadPrefabs();
    }

    void LoadPrefabs()
    {
        foreach (char c in set)
        {
            prefabMap[c + "_L"] = Resources.Load<GameObject>("Prefabs/" + c + "_L");
        }
    }

    /// <summary>
    /// Builds the codon display arrays from AvatarRegistry profiles,
    /// splitting each baseSequence into 3-character codons.
    /// </summary>
    private void BuildBaseSequenceCodons()
    {
        AvatarProfileData[] profiles = AvatarRegistry.Instance.GetAllProfiles();
        baseSequenceCodons = new string[profiles.Length][];
        for (int i = 0; i < profiles.Length; i++)
        {
            if (string.IsNullOrEmpty(profiles[i].baseSequence))
            {
                Debug.LogWarning($"Avatar profile '{profiles[i].avatarName}' has null/empty baseSequence, skipping codon split");
                baseSequenceCodons[i] = new string[0];
                continue;
            }
            baseSequenceCodons[i] = SplitIntoCodons(profiles[i].baseSequence);
        }
    }

    /// <summary>
    /// Splits a DNA sequence string into an array of 3-character codon strings.
    /// </summary>
    private string[] SplitIntoCodons(string sequence)
    {
        int codonCount = sequence.Length / 3;
        string[] codons = new string[codonCount];
        for (int i = 0; i < codonCount; i++)
            codons[i] = sequence.Substring(i * 3, 3);
        return codons;
    }

    void GetColorRangeBounds(int index, out Color lowerColor, out Color higherColor)
    {
        lowerColor = currProfileData.colorRanges[2 * index + 0];
        higherColor = currProfileData.colorRanges[2 * index + 1];
    }

    /// <summary>
    /// Sets the HSV component that the given slider controls on the current color.
    /// </summary>
    private void SetColorValue(int index, float val)
    {
        GetColorRangeBounds(index, out Color lowerColor, out Color higherColor);
        currColor[index] = Color.Lerp(lowerColor, higherColor, val);
    }

    void SetupButtons()
    {
        nextButton.onClick.AddListener(() => StartCoroutine(AdvanceStage()));
        nextButton.interactable = false;

        int buttonCount = Mathf.Min(baseSequenceCodons.Length, sequenceButtons.Length);
        for (int i = 0; i < buttonCount; i++)
        {
            int index = i;
            string str = string.Join("", baseSequenceCodons[index]);
            Transform gridObj = sequenceButtons[index].transform.Find("GridObject");
            foreach (char c in str)
            {
                Instantiate(prefabMap[c + "_L"], gridObj.transform);
            }

            sequenceButtons[index].onClick.AddListener(() =>
            {
                if (!nextButton.interactable)
                    nextButton.interactable = true;

                currBaseSequence = str;
            });
        }
        funFactButton.onClick.AddListener(() =>
        {
            FunFactUI funFactUIComp = funFactUI.GetComponent<FunFactUI>();
            string factInfo = currProfileData.info;
            funFactUIComp.Activate(currProfileData.avatarName, factInfo);
            funFactUIComp.FadeIn();
        });

        zoomOutButton.gameObject.SetActive(false);
        zoomOutButton.onClick.AddListener(() =>
        {
            cameraRig.MoveToLeft();
            zoomOutButton.gameObject.SetActive(false);
        });
    }

    private void SetupToggles()
    {
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            int index = i;
            switchToggles[index] = toggleTransforms[index].GetComponent<SwitchToggle>();
            switchToggles[index].onToggleChanged.AddListener((isOn) =>
            {
                GeneTickerItem ti = tickerItemPrefabs[index].GetComponent<GeneTickerItem>();
                if (!isOn)
                    ti.UpdateDisabledText("");
                else
                    ti.UpdateText(currProfileData.subSequences[index]);

                subSequenceEnabled[index] = isOn;
                UpdateAvatar();

                FocusToggleTarget(index, isOn);

                if (!zoomOutButton.IsActive())
                    zoomOutButton.gameObject.SetActive(true);
            });
        }
    }

    private void RotateNeedle(int i)
    {
        float range = sliders[i].maxValue - sliders[i].minValue;
        if (Mathf.Approximately(range, 0f)) return;

        float r = (sliders[i].value - sliders[i].minValue) / range;
        float deg = r * 180;

        needles[i].transform.localRotation = Quaternion.Euler(0, 0, -deg);
    }

    private void SetupSliders()
    {
        int rangeCount = currProfileData.colorRanges != null ? currProfileData.colorRanges.Length : 0;
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            if (2 * i + 1 >= rangeCount)
            {
                Debug.LogWarning($"Avatar profile missing colorRanges for subsequence {i} (has {rangeCount}, needs {2 * i + 2})");
                continue;
            }

            Color lowerColor = currProfileData.colorRanges[2 * i + 0];
            Color higherColor = currProfileData.colorRanges[2 * i + 1];

            sliders[i].value = InverseLerpColor(lowerColor, higherColor, defaultColor[i]);
            RotateNeedle(i);

            int index = i;

            string spriteName = currProfileData.id.ToString() + "_" + index.ToString();
            Sprite sprite = Resources.Load<Sprite>("Images/Dialbackground/" + spriteName);
            dials[index].sprite = sprite;

            sliders[index].onValueChanged.AddListener((value) =>
            {
                RotateNeedle(index);
                SetColorValue(index, value);

                for (int j = 0; j < NUM_CHOICES; j++)
                {
                    SetMaterialColor(targetMaterials[index, j], currColor[index]);
                }

                FocusSliderTarget(index);

                if (!zoomOutButton.IsActive())
                    zoomOutButton.gameObject.SetActive(true);
            });
        }
    }

    float InverseLerpColor(Color lowerColor, Color higherColor, Color targetColor)
    {
        float tr = (higherColor.r != lowerColor.r) 
            ? (targetColor.r - lowerColor.r) / (higherColor.r - lowerColor.r) 
            : 0f;

        float tg = (higherColor.g != lowerColor.g) 
            ? (targetColor.g - lowerColor.g) / (higherColor.g - lowerColor.g) 
            : 0f;

        float tb = (higherColor.b != lowerColor.b) 
            ? (targetColor.b - lowerColor.b) / (higherColor.b - lowerColor.b) 
            : 0f;

        int count = 0;
        float sum = 0f;

        if (higherColor.r != lowerColor.r) { sum += tr; count++; }
        if (higherColor.g != lowerColor.g) { sum += tg; count++; }
        if (higherColor.b != lowerColor.b) { sum += tb; count++; }

        if (count == 0) return 0f;

        return Mathf.Clamp01(sum / count);
    }
        
    private void ToggleCanvas()
    {
        if (currentStage + 1 < canvas.Length)
        {
            canvas[currentStage].SetActive(false);
            currentStage++;
            canvas[currentStage].SetActive(true);
            ToggleTitles();
            nextButton.interactable = true;
        }
        else
        {
            LoadGameScene();
        }
    }

    private void ToggleTitles()
    {
        if (currentStage >= 4)
            return;
        
        titleTmp.text = titles[currentStage];

        if (string.IsNullOrEmpty(subtitles[currentStage]))
            subtitleObj.SetActive(false);
        else
        {
            if (!subtitleObj.activeSelf)
                subtitleObj.SetActive(true);
            subtitleTmp.text = subtitles[currentStage];
        }
    }

    IEnumerator AdvanceStage()
    {
        nextButton.interactable = false;

        switch (currentStage)
        {
            case 0:
                LoadAvatar();
                cameraRig.MoveStraight();
                break;
            case 1:
                cameraRig.MoveToLeft();
                yield return new WaitForSeconds(1f);
                break;
            case 2:
                RevealTraits();
                yield return new WaitForSeconds(6f);
                zoomOutButton.gameObject.SetActive(false);
                cameraRig.MoveStraight();
                break;
            case 3:
                playerName = playerNameInput.text;
                playerNameInput.gameObject.SetActive(false);
                nextButton.enabled = false;

                FlickerComponent titleFlickerCo = titleTmp.GetComponent<FlickerComponent>();
                titleFlickerCo.FlickerUpdateText($"hello {playerNameInput.text.ToUpper()} !");
                waitMessage.gameObject.SetActive(true);
                yield return new WaitForSeconds(4f);
                break;
        }

        ToggleCanvas();
    }

    void RevealTraits()
    {
        for (int i = 0; i <= NUM_SUBSEQUENCES; i++)
        {
            int index = i;
            FlickerComponent flickerComponent = seqTitles[index].GetComponent<FlickerComponent>();
            string traitName = (index < NUM_SUBSEQUENCES) ? currProfileData.subSequencesTraitNames[index] : "Color";
            flickerComponent.FlickerUpdateText(traitName);
        }
    }

    private Color GetMaterialColor(Material mat)
    {
        if (mat.HasProperty(currProfileData.colorPropertyName))
        {
            return mat.GetColor(currProfileData.colorPropertyName);
        }

        return Color.black;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty(currProfileData.colorPropertyName))
        {
            mat.SetColor(currProfileData.colorPropertyName, color);
        }
    }

    /// <summary>
    /// Loads the avatar prefab matching the current base sequence from AvatarRegistry,
    /// instantiates it, and reads AvatarProfileData from the instance for direct renderer access.
    /// </summary>
    private void LoadAvatar()
    {
        GameObject prefab = AvatarRegistry.Instance.GetPrefabBySequence(currBaseSequence);
        if (prefab == null)
        {
            Debug.LogError($"No avatar prefab found for sequence: {currBaseSequence}");
            return;
        }

        AvatarProfileData prefabProfile = prefab.GetComponent<AvatarProfileData>();

        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        Vector3 pos = avatarSpawnPoint.position;
        pos.y = prefabProfile.canvasYOffset;
        avatarSpawnPoint.position = pos;

        currentAvatar = Instantiate(prefab, avatarSpawnPoint.position, avatarSpawnPoint.rotation);
        currentAvatar.transform.SetParent(avatarSpawnPoint);

        currProfileData = currentAvatar.GetComponent<AvatarProfileData>();

        InitDefaults();
        cameraRig.InitRig(currentAvatar.transform, xCamOffset, yCamOffset, zCamOffset);
    }

    /// <summary>
    /// Initializes default state from the instantiated avatar's AvatarProfileData,
    /// reading color target renderers directly instead of string-based lookups.
    /// </summary>
    private void InitDefaults()
    {
        subSequenceEnabled[0] = true;
        subSequenceEnabled[1] = true;

        int rendererCount = currProfileData.colorTargetRenderers != null ? currProfileData.colorTargetRenderers.Length : 0;
        if (rendererCount < NUM_SUBSEQUENCES * NUM_CHOICES)
            Debug.LogWarning($"Avatar profile has {rendererCount} colorTargetRenderers, expected {NUM_SUBSEQUENCES * NUM_CHOICES}");
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            for (int j = 0; j < NUM_CHOICES; j++)
            {
                int idx = i * NUM_CHOICES + j;
                if (idx < rendererCount && currProfileData.colorTargetRenderers[idx] != null)
                {
                    int matIdx = (currProfileData.colorTargetMaterialIndices != null && idx < currProfileData.colorTargetMaterialIndices.Length)
                        ? currProfileData.colorTargetMaterialIndices[idx] : 0;
                    Material[] mats = currProfileData.colorTargetRenderers[idx].materials;
                    targetMaterials[i, j] = (matIdx < mats.Length) ? mats[matIdx] : mats[0];
                }
            }
            if (targetMaterials[i, 1] != null)
            {
                defaultColor[i] = GetMaterialColor(targetMaterials[i, 1]);
                currColor[i] = defaultColor[i];
            }
        }

        GeneTickerItem ti;
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            ti = tickerItemPrefabs[i].GetComponent<GeneTickerItem>();
            ti.UpdateText(currProfileData.subSequences[i]);
        }

        ti = tickerItemPrefabs[2].GetComponent<GeneTickerItem>();
        ti.UpdateText(currProfileData.colorSequence);

        UpdateAvatar();
        SetupSliders();
    }

    private float GetAvatarHeight()
    {
        Renderer[] renderers = currentAvatar.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds.size.y;
    }

    public void FocusToggleTarget(int index, bool isOn)
    {
        if (currentAvatar == null) return;

        string[] targetPartNames = new string[]
        {
            $"{partNames[index]}1",
            $"{partNames[index]}2"
        };

        Transform[] parts = currentAvatar
            .GetComponentsInChildren<Transform>(true)
            .Where(t => targetPartNames.Contains(t.name))
            .ToArray();

        cameraRig.FocusOnComponents(parts, currProfileData.id);
    }

    public void FocusSliderTarget(int index)
    {
        if (currentAvatar == null) return;

        int activeIndex = subSequenceEnabled[index] ? 1 : 0;

        Renderer r = currProfileData.colorTargetRenderers[index * NUM_CHOICES + activeIndex]
            .GetComponentInChildren<Renderer>();

        cameraRig.FocusOnRenderer(r, currProfileData.id);
    }

    private void UpdateAvatar()
    {
        if (currentAvatar == null) return;

        for (int i = 0; i < 2; i++)
        {
            int activeIndex = subSequenceEnabled[i] ? 1 : 0;
            ToggleParts(partNames[i], activeIndex);
        }
    }

    private void TogglePart(string partName, bool enable)
    {
        Transform part = currentAvatar.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == partName);
        if (part != null)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }
        }
    }

    private void ToggleParts(string baseName, int activeIndex)
    {
        for (int i = 0; i < 2; i++)
        {
            string partName = $"{baseName}{i + 1}";
            TogglePart(partName, i == activeIndex);
        }
    }
    

    /// <summary>
    /// Builds PlayerMetadata from the current AvatarProfileData and user selections,
    /// then submits to the server for network sync.
    /// </summary>
    private void LoadGameScene()
    {
        nextButton.gameObject.SetActive(false);

        PlayerCustoms customs = new PlayerCustoms()
        {
            name = playerName,
            avatarType = currProfileData.id,
            subSeq0Enabled = subSequenceEnabled[0],
            subSeq1Enabled = subSequenceEnabled[1],
            color0 = currColor[0],
            color1 = currColor[1]
        };

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager not initialized yet");
            return;
        }

        SceneMusic music = FindObjectOfType<SceneMusic>();
        if (music != null)
            music.StartFadeOutAndDestroy(1.5f);

        // Local Cache
        PlayerLogbook.Instance.AddPlayerCustom(NetworkManager.Singleton.LocalClientId, customs);

        // Submit to server (marks this client 'ready')
        PlayerMetadataRegistry.Instance.SubmitMetadataServerRpc(customs);
    }
}
