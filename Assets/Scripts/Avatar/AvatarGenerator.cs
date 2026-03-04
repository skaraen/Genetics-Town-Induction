using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;
using Unity.Netcode;

public class AvatarGenerator : NetworkBehaviour
{
    const int NUM_SUBSEQUENCES = 2;
    const int NUM_CHOICES = 2;

    [Header("Avatar Spawn")]
    public Transform avatarSpawnPoint;
    public Transform mainCamera;

    [Header("UI References")]
    public Button[] sequenceButtons;
    public Button nextButton;
    public GameObject[] canvas;
    public GameObject[] tickerItemPrefabs;
    public TextMeshProUGUI[] seqTitles;
    public Transform[] toggleTransforms;
    public Slider[] sliders;
    public Image[] dials;
    public Image[] needles;

    [SerializeField] private int cameraFocus;
    [SerializeField] Button funFactButton;
    [SerializeField] GameObject funFactUI;

    [SerializeField] TMP_InputField playerNameInput;

    const string set = "ATGC";
    private string[][] baseSequenceCodons;
    private Dictionary<string, GameObject> prefabMap;

    private Vector3 straightFocus = new Vector3(0, 0, -6);
    private Vector3 sideFocus = new Vector3(-3, 0, -6);

    private Vector3 headFocus = new Vector3(-3, 0.5f, -6);
    private Vector3 torsoFocus = new Vector3(-3, -0.5f, -6);
    private Vector3 velocity = Vector3.zero;


    private string currBaseSequence;
    private AvatarProfileData currProfileData;
    private GameObject currentAvatar;
    private Vector3[] defaultColor;  // HSV stored as (x=h, y=s, z=v)
    private Vector3[] currColor;     // HSV stored as (x=h, y=s, z=v)
    private bool[] subSequenceEnabled;
    private SwitchToggle[] switchToggles;
    private Material[,] targetMaterials;
    private string playerName;

    private int currentStage = 0;

    void Start()
    {
        defaultColor = new Vector3[NUM_SUBSEQUENCES];
        currColor = new Vector3[NUM_SUBSEQUENCES];
        subSequenceEnabled = new bool[NUM_SUBSEQUENCES];
        switchToggles = new SwitchToggle[NUM_SUBSEQUENCES];
        targetMaterials = new Material[NUM_SUBSEQUENCES, NUM_CHOICES];
        cameraFocus = 0;

        BuildBaseSequenceCodons();
        ArrayUtils.ShuffleRows(baseSequenceCodons);

        canvas[0].SetActive(true);
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


    void LateUpdate()
    {
        Vector3 targetPosition;
        switch (cameraFocus)
        {
            case 0:
                targetPosition = straightFocus;
                break;
            case 1:
                targetPosition = sideFocus;
                break;
            case 2:
                targetPosition = headFocus;
                break;
            case 3:
                targetPosition = torsoFocus;
                break;
            default:
                targetPosition = straightFocus;
                break;
        }

        if (targetPosition != mainCamera.position)
            mainCamera.position = Vector3.SmoothDamp(mainCamera.position, targetPosition, ref velocity, 0.3f);
    }

    /// <summary>
    /// Returns whether the given slider adjusts Value instead of Hue.
    /// Falls back to false (Hue mode) if the array is unset or too short.
    /// </summary>
    private bool SliderUsesValue(int sliderIndex)
    {
        if (currProfileData.colorSliderUsesValue == null || sliderIndex >= currProfileData.colorSliderUsesValue.Length)
            return false;
        return currProfileData.colorSliderUsesValue[sliderIndex];
    }

    /// <summary>
    /// Extracts the HSV component that the given slider controls (Hue or Value).
    /// </summary>
    private float GetSliderValue(int sliderIndex, Vector3 hsv)
    {
        if (SliderUsesValue(sliderIndex))
            return hsv.z;

        return hsv.x;
    }

    /// <summary>
    /// Sets the HSV component that the given slider controls on the current color.
    /// </summary>
    private void SetColorValue(int index, float val)
    {
        if (SliderUsesValue(index))
            currColor[index].z = val;
        else
            currColor[index].x = val;
    }

    void SetupButtons()
    {
        nextButton.onClick.AddListener(() => AdvanceStage());

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
                currBaseSequence = str;
            });
        }
        funFactButton.onClick.AddListener(() =>
        {
            FunFactUI funFactUIComp = funFactUI.GetComponent<FunFactUI>();
            string factInfo = "The base sequence you chose contributes to " + currProfileData.info;
            funFactUIComp.Activate(currProfileData.avatarName, factInfo);
            funFactUIComp.FadeIn();
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
            Color.RGBToHSV(currProfileData.colorRanges[2 * i + 0], out float minH, out float minS, out float minV);
            Vector3 minColor = new Vector3(minH, minS, minV);
            Color.RGBToHSV(currProfileData.colorRanges[2 * i + 1], out float maxH, out float maxS, out float maxV);
            Vector3 maxColor = new Vector3(maxH, maxS, maxV);
            float minVal = GetSliderValue(i, minColor);
            float maxVal = GetSliderValue(i, maxColor);
            // Ensure min <= max so the slider isn't stuck when color ranges are ordered bright-to-dark
            sliders[i].minValue = Mathf.Min(minVal, maxVal);
            sliders[i].maxValue = Mathf.Max(minVal, maxVal);
            float colorValue = GetSliderValue(i, defaultColor[i]);

            Debug.Log($"(Slider {i + 1}) Min value: {sliders[i].minValue}, Max value: {sliders[i].maxValue} ");
            sliders[i].value = Mathf.Clamp(colorValue, sliders[i].minValue, sliders[i].maxValue);
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
                    SetMaterialColor(targetMaterials[index, j], Color.HSVToRGB(currColor[index].x, currColor[index].y, currColor[index].z, true));
                }
            });
        }
    }

    IEnumerator DelayedReveal(float revealDelay, float toggleDelay)
    {
        RevealTraits();
        yield return new WaitForSeconds(revealDelay);

        cameraFocus = 0;
        ToggleCanvas();
    }

    IEnumerator DelayedToggleCanvas(float delay)
    {
        yield return new WaitForSeconds(delay);
        ToggleCanvas();
    }

    private void ToggleCanvas()
    {
        if (currentStage + 1 < canvas.Length)
        {
            canvas[currentStage].SetActive(false);
            currentStage++;
            canvas[currentStage].SetActive(true);
        }
        else
        {
            LoadGameScene();
        }
    }

    void AdvanceStage()
    {
        switch (currentStage)
        {
            case 0:
                LoadAvatar();
                ToggleCanvas();
                break;
            case 1:
                cameraFocus = 1;
                StartCoroutine(DelayedToggleCanvas(1f));
                break;
            case 2:
                StartCoroutine(DelayedReveal(6f, 1f));
                break;
            case 3:
                playerName = playerNameInput.text;
                StartCoroutine(DelayedToggleCanvas(1f));
                break;
        }
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
        pos.y = prefabProfile.spawnYOffset;
        avatarSpawnPoint.position = pos;

        currentAvatar = Instantiate(prefab, avatarSpawnPoint.position, avatarSpawnPoint.rotation);
        currentAvatar.transform.SetParent(avatarSpawnPoint);

        currProfileData = currentAvatar.GetComponent<AvatarProfileData>();

        InitDefaults();
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
                    targetMaterials[i, j] = currProfileData.colorTargetRenderers[idx].material;
            }
            if (targetMaterials[i, 0] != null)
            {
                Color.RGBToHSV(GetMaterialColor(targetMaterials[i, 0]), out float h, out float s, out float v);
                defaultColor[i] = new Vector3(h, s, v);
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

    private void UpdateAvatar()
    {
        if (currentAvatar == null) return;

        string[] parts = { "head", "torso" };

        for (int i = 0; i < 2; i++)
        {
            int activeIndex = subSequenceEnabled[i] ? 1 : 0;
            ToggleParts(parts[i], activeIndex);
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
        PlayerMetadata metadata = new PlayerMetadata(currProfileData);
        metadata.name = playerName;

        for (int i = 0; i < 2; i++)
        {
            string subSeq = currProfileData.subSequences[i];
            metadata.sequenceEnabled[i] = subSequenceEnabled[i];
            metadata.subSequences[i] = subSequenceEnabled[i] ? "ATG" + subSeq : subSeq + "TAG";
            metadata.colors[i] = Color.HSVToRGB(currColor[i].x, currColor[i].y, currColor[i].z, true);
        }

        // Optional local cache
        PlayerMetadataManager.Instance.AddPlayerMetadata(metadata);

        // Submit to server (marks this client 'ready')
        var dto = MetaDataCustomsMapper.ToNet(metadata);
        PlayerMetadataRegistry.Instance.SubmitMetadataServerRpc(dto);

        // DO NOT call SceneManager.LoadScene here.
        // The server will transition everyone via NetworkManager.SceneManager.LoadScene(...)
        // when AllPlayersReady() is true.
    }
}
