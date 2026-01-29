using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class AvatarManager : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject seaAvatarPrefab;
    public GameObject mountainAvatarPrefab;
    public GameObject skyAvatarPrefab;
    public GameObject forestAvatarPrefab;

    [Header("Avatar Spawn")]
    public Transform avatarSpawnPoint;

    [Header("UI References")]
    public Button[] biomeButtons;
    public Button[] colorButtons;
    public Button intelligenceLeftButton;
    public Button intelligenceRightButton;
    public Button abilityLeftButton;
    public Button abilityRightButton;
    public Button goButton;

    public TextMeshProUGUI intelligenceText;
    public TextMeshProUGUI abilityText;
    public TextMeshProUGUI infoText;

    private GameObject currentAvatar;
    private string currentBiome = "Sea";
    private string currentIntelligence = "Aquatic";
    private string currentAbility = "Aquatic";
    private Color currentColor = Color.white;
    private Color defaultColor;

    private string[] intelligenceOptions = { "Aquatic", "Smart", "Primitive" };
    private string[] abilityOptions = { "Aquatic", "Flying", "Ground" };

    private int intelligenceIndex = 0;
    private int abilityIndex = 0;

    void Start()
    {
        SetupButtons();
        CreateAvatar();
        UpdateAvatar();
    }

    void SetupButtons()
    {
        biomeButtons[0].onClick.AddListener(() => SetBiome("Sea"));
        biomeButtons[1].onClick.AddListener(() => SetBiome("Mountain"));
        biomeButtons[2].onClick.AddListener(() => SetBiome("Sky"));
        biomeButtons[3].onClick.AddListener(() => SetBiome("Forest"));

        foreach (Button btn in colorButtons)
        {
            btn.onClick.AddListener(() => SetColor(btn));
        }

        intelligenceLeftButton.onClick.AddListener(() => ChangeIntelligence(-1));
        intelligenceRightButton.onClick.AddListener(() => ChangeIntelligence(1));

        abilityLeftButton.onClick.AddListener(() => ChangeAbility(-1));
        abilityRightButton.onClick.AddListener(() => ChangeAbility(1));

        goButton.onClick.AddListener(OnGoButtonClicked);
    }

    void SetBiome(string biome)
    {
        currentBiome = biome;
        ResetButtonOutlines(biomeButtons);

        foreach (var btn in biomeButtons)
        {
            if (btn.name.Contains(biome))
            {
                HighlightButton(btn);
                break;
            }
        }

        switch (currentBiome)
        {
            case "Sea":
                infoText.text = "Jellyfish have existed for over 600 million years, predating dinosaurs, and are found in every ocean!";
                break;
            case "Mountain":
                infoText.text = "Mountain goats are native to North America, inhabiting high-altitude environments like alpine and subalpine regions.";
                break;
            case "Sky":
                infoText.text = "Hummingbirds are the smallest birds, with some species, like the bee hummingbird, measuring only about an inch long.";
                break;
            case "Forest":
                infoText.text = "Axolotls are known for their remarkable ability to regenerate lost body parts, including limbs, spinal cords, and even parts of their brains!";
                break;
            default:
                infoText.text = "";
                break;
        }

        CreateAvatar();
        UpdateAvatar();
    }

    void SetColor(Button btn)
    {
        currentColor = btn.GetComponent<Image>().color;
        if (btn.name.Contains("Default"))
        {
            currentColor = defaultColor;
        }

        ResetButtonOutlines(colorButtons);
        HighlightButton(btn);

        UpdateAvatar();
    }

    void InitDefaults()
    {
        switch (currentBiome)
        {
            case "Sea":
                defaultColor = new Color(0.682f, 0f, 1f);
                intelligenceOptions = new string[] { "Camouflage", "Bioelectricity" };
                abilityOptions = new string[] { "Burst Swim", "Poison" };
                break;

            case "Mountain":
                defaultColor = Color.white;
                intelligenceOptions = new string[] { "Balance", "Rumination" };
                abilityOptions = new string[] { "Insulation", "Durability" };
                break;

            case "Sky":
                defaultColor = new Color(0.5f, 0.7f, 1f);
                intelligenceOptions = new string[] { "Smart", "Primitive" };
                abilityOptions = new string[] { "Flying" };
                break;

            case "Forest":
                defaultColor = new Color(0.2f, 0.6f, 0.3f);
                intelligenceOptions = new string[] { "Primitive", "Smart" };
                abilityOptions = new string[] { "Ground", "Flying" };
                break;

            default:
                defaultColor = Color.white;
                intelligenceOptions = new string[] { "Aquatic", "Smart", "Primitive" };
                abilityOptions = new string[] { "Aquatic", "Flying", "Ground" };
                break;
        }

        intelligenceIndex = 0;
        abilityIndex = 0;

        currentIntelligence = intelligenceOptions[intelligenceIndex];
        currentAbility = abilityOptions[abilityIndex];

        intelligenceText.text = currentIntelligence;
        abilityText.text = currentAbility;

        currentColor = defaultColor;
        UpdateAvatar();
    }

    void ChangeIntelligence(int direction)
    {
        intelligenceIndex = (intelligenceIndex + direction + intelligenceOptions.Length) % intelligenceOptions.Length;
        currentIntelligence = intelligenceOptions[intelligenceIndex];
        intelligenceText.text = currentIntelligence;

        // Add your intelligence facts here as needed
        UpdateAvatar();
    }

    void ChangeAbility(int direction)
    {
        abilityIndex = (abilityIndex + direction + abilityOptions.Length) % abilityOptions.Length;
        currentAbility = abilityOptions[abilityIndex];
        abilityText.text = currentAbility;

        // Add your ability facts here as needed
        UpdateAvatar();
    }

    void CreateAvatar()
    {
        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        GameObject prefab = null;
        switch (currentBiome)
        {
            case "Sea": prefab = seaAvatarPrefab; break;
            case "Mountain": prefab = mountainAvatarPrefab; break;
            case "Sky": prefab = skyAvatarPrefab; break;
            case "Forest": prefab = forestAvatarPrefab; break;
        }

        if (prefab != null)
        {
            Vector3 pos = avatarSpawnPoint.position;
            pos.y = (currentBiome == "Sea") ? -3 : -1;
            avatarSpawnPoint.position = pos;

            currentAvatar = Instantiate(prefab, avatarSpawnPoint.position, avatarSpawnPoint.rotation);
            currentAvatar.transform.SetParent(avatarSpawnPoint);
        }

        InitDefaults();
    }

    void UpdateAvatar()
    {
        if (currentAvatar == null) return;

        HashSet<string> targetNames = GetColorTargets();
        Renderer[] renderers = currentAvatar.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (targetNames.Contains(r.gameObject.name))
            {
                Material mat = r.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", currentColor);
                }
                if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", currentColor);
                }
            }
        }

        ToggleParts("head", intelligenceIndex, 2);
        ToggleParts("torso", abilityIndex, 2);
    }

    HashSet<string> GetColorTargets()
    {
        HashSet<string> colorTargets = new HashSet<string>();

        switch (currentBiome)
        {
            case "Sea":
                colorTargets.Add("head1Emission");
                colorTargets.Add("head2Emission");
                break;
            case "Mountain":
                colorTargets.Add("torso1");
                colorTargets.Add("torso2");
                break;
            case "Sky":
                colorTargets.Add("wings/emission");
                colorTargets.Add("body/emission");
                break;
            case "Forest":
                colorTargets.Add("leaves/emission");
                colorTargets.Add("trunk/emission");
                break;
        }

        return colorTargets;
    }

    void TogglePart(string partName, bool enable)
    {
        Transform part = currentAvatar.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == partName);
        if (part != null)
        {
            part.gameObject.SetActive(enable);
        }
    }

    void ToggleParts(string baseName, int activeIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            string partName = $"{baseName}{i + 1}";
            TogglePart(partName, i == activeIndex);
        }
    }

    void ResetButtonOutlines(Button[] buttons)
    {
        foreach (var btn in buttons)
        {
            var outline = btn.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }

    void HighlightButton(Button button)
    {
        var outline = button.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    void OnGoButtonClicked()
    {
        AvatarDataHolder.biome = currentBiome;
        AvatarDataHolder.intelligence = currentIntelligence;
        AvatarDataHolder.ability = currentAbility;
        AvatarDataHolder.color = currentColor;

        Debug.Log("Avatar data saved: " + AvatarDataHolder.biome + ", " + AvatarDataHolder.intelligence + ", " + AvatarDataHolder.ability);

        SceneManager.LoadScene("GameScene_Karaen");
    }
}
