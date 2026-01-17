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
    const int NUM_CANVAS = 3;
    const int NUM_AVATARS = 4;

    [Header("Avatar Spawn")]
    public Transform avatarSpawnPoint;
    public Transform mainCamera;

    [Header("UI References")]
    public Button[] sequenceButtons;
    public Button[] goButtons;
    public GameObject[] canvas;
    public TextMeshProUGUI infoText;
    public GameObject[] tickerItemPrefabs;
    public Transform[] toggleTransforms;
    public Slider[] sliders;
    public Image[] needles;

    [SerializeField] private int cameraFocus;

    private string[][] baseSequences = new string[][]
    {
        new string[] { "AGT","AAA","GGA","GAA","GAA","CTT","TTC","ACT","GGA","GTT","GTC"},
        new string[] { "GCA","TGT","TGC","GGA","GTG","ATT","AGT","GGG","TTT","GAA","AAG"},
        new string[] { "ACA","GAT","TTC","TTA","TCG","GCT","TCC","ACA","CTG","CGG","TAG"},
        new string[] { "GGG","ACC","TTG","AGA","GCG","GCC","AGG","CCA","GCC","TCG","GAG"}
    };

    private Vector3 straightFocus = new Vector3(0, 0, -6);
    private Vector3 sideFocus = new Vector3(-3, 0, -6);

    private Vector3 headFocus = new Vector3(-3, 0.5f, -6);
    private Vector3 torsoFocus = new Vector3(-3, -0.5f, -6);
    private Vector3 velocity = Vector3.zero;


    private string currBaseSequence;
    private AvatarProfile currProfile;
    private GameObject currentAvatar;
    private ColorHSV[] defaultColor;
    private ColorHSV[] currColor;
    private bool[] subSequenceEnabled;
    private SwitchToggle[] switchToggles;
    private Material[,] targetMaterials;

    void Start()
    {
        defaultColor = new ColorHSV[NUM_SUBSEQUENCES];
        currColor = new ColorHSV[NUM_SUBSEQUENCES];
        subSequenceEnabled = new bool[NUM_SUBSEQUENCES];
        switchToggles = new SwitchToggle[NUM_SUBSEQUENCES];
        targetMaterials = new Material[NUM_SUBSEQUENCES, NUM_CHOICES];
        cameraFocus = 0;

        ArrayUtils.ShuffleRows(baseSequences);

        canvas[0].SetActive(true);
        for (int i = 1; i < NUM_CANVAS; i++)
        {
            canvas[i].SetActive(false);
        }
        SetupButtons();
        SetupToggles();
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

    private float GetSliderValue(ColorHSV hsv)
    {
        if (currProfile.id == 2)
            return hsv.v;

        return hsv.h;
    }

    private void SetColorValue(int index, float val)
    {
        if (currProfile.id == 2)
            currColor[index].v = val;
        else
            currColor[index].h = val;
    }

    void SetupButtons()
    {
        for (int i = 0; i < NUM_CANVAS; i++)
        {
            int index = i;
            goButtons[i].onClick.AddListener(() => SetCanvas(index));
        }

        for (int i = 0; i < NUM_AVATARS; i++)
        {
            int index = i;
            sequenceButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = string.Join("\n", baseSequences[index]);
            sequenceButtons[index].onClick.AddListener(() =>
            {
                currBaseSequence = string.Join("", baseSequences[index]);
            });
        }
    }

    private void SetupToggles()
    {
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            int index = i;
            switchToggles[index] = toggleTransforms[index].GetComponent<SwitchToggle>();
            switchToggles[index].onToggleChanged.AddListener((isOn) =>
            {
                subSequenceEnabled[index] = isOn;
                UpdateAvatar();
            });
        }
    }

    private void RotateNeedle(int i)
    {
        float r = (sliders[i].value - sliders[i].minValue) / (sliders[i].maxValue - sliders[i].minValue);
        float deg = r * 180;

        needles[i].transform.localRotation = Quaternion.Euler(0, 0, -deg);
    }

    private void SetupSliders()
    {
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            ColorHSV minColor = ColorUtils.GetHSVFromHex(currProfile.colorRanges[2 * i + 0]);
            sliders[i].minValue = GetSliderValue(minColor);
            ColorHSV maxColor = ColorUtils.GetHSVFromHex(currProfile.colorRanges[2 * i + 1]);
            sliders[i].maxValue = GetSliderValue(maxColor);
            float colorValue = GetSliderValue(defaultColor[i]);

            Debug.Log($"(Slider {i + 1}) Min value: {sliders[i].minValue}, Max value: {sliders[i].maxValue} ");
            sliders[i].value = Mathf.Clamp(colorValue, sliders[i].minValue, sliders[i].maxValue);
            RotateNeedle(i);

            int index = i;
            sliders[index].onValueChanged.AddListener((value) =>
            {
                RotateNeedle(index);
                SetColorValue(index, value);

                for (int j = 0; j < NUM_CHOICES; j++)
                {
                    SetMaterialColor(targetMaterials[index, j], ColorUtils.GetRGBFromHSV(currColor[index]));
                }
            });
        }
    }

    IEnumerator DelayedToggleCanvas(int canvasId)
    {
        yield return new WaitForSeconds(1f);
        canvas[canvasId].SetActive(false);

        if (canvasId + 1 < canvas.Length)
        {
            canvas[canvasId + 1].SetActive(true);
        }
    }

    private void ToggleCanvas(int canvasId)
    {
        canvas[canvasId].SetActive(false);

        if (canvasId + 1 < canvas.Length)
        {
            canvas[canvasId + 1].SetActive(true);
        }
        else
        {
            LoadGameScene();
        }
    }

    void SetCanvas(int canvasId)
    {
        switch (canvasId)
        {
            case 0:
                LoadAvatar();
                ToggleCanvas(canvasId);
                break;
            case 1:
                cameraFocus = 1;
                StartCoroutine(DelayedToggleCanvas(canvasId));
                break;
            case 2:
                LoadGameScene();
                break;
        }
    }

    private Color GetMaterialColor(Material mat)
    {
        string propertyName = (currProfile.id == 2) ? "_BaseColor" : "_EmissionColor";
        if (mat.HasProperty(propertyName))
        {
            Color matColor = mat.GetColor(propertyName);

            return matColor;
        }

        return Color.black;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        string propertyName = (currProfile.id == 2) ? "_BaseColor" : "_EmissionColor";
        if (mat.HasProperty(propertyName))
        {
            mat.SetColor(propertyName, color);
        }
    }

    private void LoadAvatar()
    {
        currProfile = AvatarProfileManager.Instance.GetProfileBySequence(currBaseSequence);
        if (currProfile == null)
            Debug.Log($"Currprofile null: {currBaseSequence}");
        infoText.text = "The base sequence you chose contributes to " + currProfile.info;

        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        GameObject prefab = Resources.Load<GameObject>("Avatars/" + currProfile.prefabPath);

        if (prefab != null)
        {
            Vector3 pos = avatarSpawnPoint.position;
            pos.y = (currProfile.id != 2) ? -2 : -1;
            avatarSpawnPoint.position = pos;
            // avatarSpawnPoint.forward = new Vector3(1, 0, 0);

            currentAvatar = Instantiate(prefab, avatarSpawnPoint.position, prefab.transform.rotation);
            currentAvatar.transform.SetParent(avatarSpawnPoint);
        }

        InitDefaults();
    }

    private void InitDefaults()
    {
        subSequenceEnabled[0] = true;
        subSequenceEnabled[1] = true;

        Renderer[] renderers = currentAvatar.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            for (int j = 0; j < NUM_CHOICES; j++)
            {
                // read gameobject names from json, such as 'head1emission', etc, that are defined to be colored
                targetMaterials[i, j] = renderers.FirstOrDefault(r => r.gameObject.name == currProfile.colorTargets[i * NUM_SUBSEQUENCES + j]).material;
            }
            defaultColor[i] = ColorUtils.GetHSVFromRGB(GetMaterialColor(targetMaterials[i, 0]));
            currColor[i] = new ColorHSV(defaultColor[i].h, defaultColor[i].s, defaultColor[i].v);
        }

        TickerItem ti;
        for (int i = 0; i < NUM_SUBSEQUENCES; i++)
        {
            ti = tickerItemPrefabs[i].GetComponent<TickerItem>();
            ti.UpdateText(currProfile.subSequences[i]);
        }

        ti = tickerItemPrefabs[2].GetComponent<TickerItem>();
        ti.UpdateText(currProfile.colorSequence);

        UpdateAvatar();
        SetupSliders();
    }

    private void UpdateAvatar()
    {
        if (currentAvatar == null) return;

        Renderer[] renderers = currentAvatar.GetComponentsInChildren<Renderer>();

        // foreach (Renderer r in renderers)
        // {
        //     if (targetNames.Contains(r.gameObject.name))
        //     {
        //         Material mat = r.material;
        //         if (mat.HasProperty("_BaseColor"))
        //         {
        //             mat.SetColor("_BaseColor", currColor);
        //         }
        //         if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
        //         {
        //             mat.SetColor("_EmissionColor", currColor);
        //         }
        //     }
        // }

        string[] parts = { "head", "torso" };

        for (int i = 0; i < 2; i++)
        {
            int activeIndex = subSequenceEnabled[i] ? 1 : 0;
            ToggleParts(parts[i], activeIndex);
        }
    }

    private void TogglePart(string partName, bool enable)
    {
        Transform part = currentAvatar.transform.Find(partName);
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
    /// Gets a placeholder name (should be user selected in future) for multiplayer
    /// based on avatar typ
    /// </summary>
    /// <param name="avatarType"></param>
    /// <returns></returns>
    private string GetMultiplayerName(int avatarType)
    {
        switch (avatarType)
        {
            case 1:
                return "Katara";
            case 2:
                return "Toph";
            case 3:
                return "Aang";
            case 4:
                return "Zuko";
        }
        return "Ozai";
    }

    private void LoadGameScene()
    {
        // Build your local metadata from current UI/profile
        PlayerMetadata metadata = new PlayerMetadata(currProfile);
        metadata.name = GetMultiplayerName(currProfile.id);

        for (int i = 0; i < 2; i++)
        {
            string subSeq = currProfile.subSequences[i];
            metadata.sequenceEnabled[i] = subSequenceEnabled[i];
            metadata.subSequences[i] = subSequenceEnabled[i] ? "ATG" + subSeq : subSeq + "TAG";
            metadata.colors[i] = ColorUtils.GetRGBFromHSV(currColor[i]);
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
