using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GeneticAvatarCreator : MonoBehaviour
{
    [Header("UI References")]
    public Canvas avatarCreatorCanvas;
    public Transform avatarSpawnPoint;
    public Camera avatarCamera;
    
    [Header("Controls Panel")]
    public GameObject controlsPanel;
    public TextMeshProUGUI speciesSequenceText;
    public TextMeshProUGUI colorSequenceText;
    public TextMeshProUGUI sizeSequenceText;
    public TextMeshProUGUI moveSequenceText;
    public Slider sizeSlider;
    public TextMeshProUGUI sizeValueText;
    
    [Header("Avatar Info Panel")]
    public GameObject avatarInfoPanel;
    public TextMeshProUGUI speciesNameText;
    public TextMeshProUGUI sizeDisplayText;
    public TextMeshProUGUI colorNameText;
    public TextMeshProUGUI movementNameText;
    public TextMeshProUGUI similarityText;
    
    [Header("Species Buttons")]
    public Button fireflyButton;
    public Button eelButton;
    public Button jellyfishButton;
    public Button whaleButton;
    public Button batButton;
    
    [Header("Color Buttons")]
    public Button pinkButton;
    public Button greenButton;
    public Button purpleButton;
    public Button orangeButton;
    public Button blueButton;
    
    [Header("Movement Buttons")]
    public Button floatButton;
    public Button swimButton;
    public Button bounceButton;
    public Button spiralButton;
    
    [Header("Other Buttons")]
    public Button randomDNAButton;
    public Button closeButton;
    
    [Header("Avatar Prefabs")]
    public GameObject fireflyPrefab;
    public GameObject eelPrefab;
    public GameObject jellyfishPrefab;
    public GameObject whalePrefab;
    public GameObject batPrefab;
    
    [Header("Background DNA Particles")]
    public GameObject dnaParticlePrefab;
    public Transform dnaParticleContainer;
    
    // Current avatar state
    private GameObject currentAvatar;
    private string currentSpecies = "firefly";
    private float currentSize = 1.0f;
    private Color currentColor = Color.magenta;
    private string movementPattern = "float";
    private List<GameObject> dnaParticles = new List<GameObject>();
    private List<Button> allButtons = new List<Button>();
    
    // Movement animation variables
    private Vector3 originalAvatarPosition;
    private float animationTimer = 0f;
    
    // Species data
    private Dictionary<string, SpeciesData> speciesData = new Dictionary<string, SpeciesData>
    {
        {"firefly", new SpeciesData("Firefly", Color.yellow)},
        {"eel", new SpeciesData("Electric Eel", Color.blue)},
        {"jellyfish", new SpeciesData("Jellyfish", Color.magenta)},
        {"whale", new SpeciesData("Whale", new Color(0.27f, 0.51f, 0.71f))},
        {"bat", new SpeciesData("Bat", Color.gray)}
    };
    
    private Dictionary<Color, string> colorNames = new Dictionary<Color, string>
    {
        {Color.magenta, "Pink"},
        {Color.green, "Green"},
        {new Color(0.58f, 0.44f, 0.86f), "Purple"},
        {new Color(1f, 0.55f, 0f), "Orange"},
        {new Color(0.12f, 0.56f, 1f), "Blue"}
    };

    void Start()
    {
        InitializeUI();
        CreateDNABackground();
        CreateAvatar();
        UpdateUI();
        
        // Hide the minigame initially
        avatarCreatorCanvas.gameObject.SetActive(false);
    }
    
    void Update()
    {
        if (avatarCreatorCanvas.gameObject.activeInHierarchy)
        {
            AnimateAvatar();
            AnimateDNAParticles();
        }
    }
    
    void InitializeUI()
    {
        // Store all buttons for easy management
        allButtons.AddRange(new Button[] {
            fireflyButton, eelButton, jellyfishButton, whaleButton, batButton,
            pinkButton, greenButton, purpleButton, orangeButton, blueButton,
            floatButton, swimButton, bounceButton, spiralButton
        });
        
        // Setup button listeners
        fireflyButton.onClick.AddListener(() => SetSpecies("ATCGATCG", "firefly", fireflyButton));
        eelButton.onClick.AddListener(() => SetSpecies("GGCCTTAA", "eel", eelButton));
        jellyfishButton.onClick.AddListener(() => SetSpecies("TTAACCGG", "jellyfish", jellyfishButton));
        whaleButton.onClick.AddListener(() => SetSpecies("CCGGAATT", "whale", whaleButton));
        batButton.onClick.AddListener(() => SetSpecies("AATTCCGG", "bat", batButton));
        
        pinkButton.onClick.AddListener(() => SetColor("CCTTAAGG", Color.magenta, pinkButton));
        greenButton.onClick.AddListener(() => SetColor("TTGGCCAA", Color.green, greenButton));
        purpleButton.onClick.AddListener(() => SetColor("AAGGCCTT", new Color(0.58f, 0.44f, 0.86f), purpleButton));
        orangeButton.onClick.AddListener(() => SetColor("GGCCAATT", new Color(1f, 0.55f, 0f), orangeButton));
        blueButton.onClick.AddListener(() => SetColor("AATTGGCC", new Color(0.12f, 0.56f, 1f), blueButton));
        
        floatButton.onClick.AddListener(() => SetMovement("float", floatButton));
        swimButton.onClick.AddListener(() => SetMovement("swim", swimButton));
        bounceButton.onClick.AddListener(() => SetMovement("bounce", bounceButton));
        spiralButton.onClick.AddListener(() => SetMovement("spiral", spiralButton));
        
        randomDNAButton.onClick.AddListener(RandomDNA);
        closeButton.onClick.AddListener(CloseMinigame);
        
        sizeSlider.onValueChanged.AddListener(UpdateSize);
        
        // Set initial active buttons
        fireflyButton.GetComponent<Image>().color = Color.yellow;
        pinkButton.GetComponent<Image>().color = Color.yellow;
        floatButton.GetComponent<Image>().color = Color.yellow;
    }
    
    void CreateAvatar()
    {
        if (currentAvatar != null)
        {
            DestroyImmediate(currentAvatar);
        }
        
        GameObject prefabToSpawn = null;
        switch(currentSpecies)
        {
            case "firefly": prefabToSpawn = fireflyPrefab; break;
            case "eel": prefabToSpawn = eelPrefab; break;
            case "jellyfish": prefabToSpawn = jellyfishPrefab; break;
            case "whale": prefabToSpawn = whalePrefab; break;
            case "bat": prefabToSpawn = batPrefab; break;
        }
        
        if (prefabToSpawn != null)
        {
            currentAvatar = Instantiate(prefabToSpawn, avatarSpawnPoint.position, avatarSpawnPoint.rotation);
            currentAvatar.transform.SetParent(avatarSpawnPoint);
            originalAvatarPosition = currentAvatar.transform.localPosition;
            
            // Apply current modifications
            currentAvatar.transform.localScale = Vector3.one * currentSize;
            
            // Apply color to all renderers
            Renderer[] renderers = currentAvatar.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = currentColor;
            }
        }
    }
    
    void CreateDNABackground()
    {
        // Clear existing particles
        foreach (GameObject particle in dnaParticles)
        {
            if (particle != null) DestroyImmediate(particle);
        }
        dnaParticles.Clear();
        
        // Create new particles
        for (int i = 0; i < 25; i++)
        {
            GameObject particle = Instantiate(dnaParticlePrefab, dnaParticleContainer);
            
            // Random position around the avatar
            Vector3 randomPos = new Vector3(
                Random.Range(-15f, 15f),
                Random.Range(-10f, 10f),
                Random.Range(-8f, 2f)
            );
            particle.transform.localPosition = randomPos;
            
            // Random color
            Color particleColor = Random.value > 0.5f ? new Color(0.53f, 0.81f, 0.92f, 0.4f) : new Color(0.6f, 0.98f, 0.6f, 0.4f);
            particle.GetComponent<Renderer>().material.color = particleColor;
            
            dnaParticles.Add(particle);
        }
    }
    
    void SetSpecies(string sequence, string species, Button clickedButton)
    {
        currentSpecies = species;
        speciesSequenceText.text = sequence;
        speciesNameText.text = speciesData[species].name;
        
        // Update button states
        ResetButtonColors(new Button[] {fireflyButton, eelButton, jellyfishButton, whaleButton, batButton});
        clickedButton.GetComponent<Image>().color = Color.yellow;
        
        CreateAvatar();
        UpdateSimilarity();
    }
    
    void SetColor(string sequence, Color color, Button clickedButton)
    {
        currentColor = color;
        colorSequenceText.text = sequence;
        colorNameText.text = colorNames[color];
        
        // Update button states
        ResetButtonColors(new Button[] {pinkButton, greenButton, purpleButton, orangeButton, blueButton});
        clickedButton.GetComponent<Image>().color = Color.yellow;
        
        CreateAvatar();
        UpdateSimilarity();
    }
    
    void UpdateSize(float value)
    {
        currentSize = value;
        sizeValueText.text = value.ToString("F1");
        
        string[] sizeNames = {"Tiny", "Small", "Normal", "Large", "Huge", "Giant"};
        int sizeIndex = Mathf.Clamp(Mathf.FloorToInt((value - 0.5f) / 0.5f), 0, sizeNames.Length - 1);
        sizeDisplayText.text = sizeNames[sizeIndex];
        
        if (currentAvatar != null)
        {
            currentAvatar.transform.localScale = Vector3.one * currentSize;
        }
        
        UpdateSimilarity();
    }
    
    void SetMovement(string pattern, Button clickedButton)
    {
        movementPattern = pattern;
        movementNameText.text = pattern.Substring(0, 1).ToUpper() + pattern.Substring(1);
        
        // Update button states
        ResetButtonColors(new Button[] {floatButton, swimButton, bounceButton, spiralButton});
        clickedButton.GetComponent<Image>().color = Color.yellow;
        
        UpdateSimilarity();
    }
    
    void RandomDNA()
    {
        // Random species
        string[] species = {"firefly", "eel", "jellyfish", "whale", "bat"};
        Button[] speciesButtons = {fireflyButton, eelButton, jellyfishButton, whaleButton, batButton};
        int randomSpeciesIndex = Random.Range(0, species.Length);
        SetSpecies("RANDOM" + Random.Range(1000, 9999), species[randomSpeciesIndex], speciesButtons[randomSpeciesIndex]);
        
        // Random color
        Color[] colors = {Color.magenta, Color.green, new Color(0.58f, 0.44f, 0.86f), new Color(1f, 0.55f, 0f), new Color(0.12f, 0.56f, 1f)};
        Button[] colorButtons = {pinkButton, greenButton, purpleButton, orangeButton, blueButton}; 
        int randomColorIndex = Random.Range(0, colors.Length);
        SetColor("RAND" + Random.Range(1000, 9999), colors[randomColorIndex], colorButtons[randomColorIndex]);
        
        // Random size
        float randomSize = Random.Range(0.5f, 3.0f);
        sizeSlider.value = randomSize;
        UpdateSize(randomSize);
        
        // Random movement
        string[] movements = {"float", "swim", "bounce", "spiral"};
        Button[] movementButtons = {floatButton, swimButton, bounceButton, spiralButton};
        int randomMovementIndex = Random.Range(0, movements.Length);
        SetMovement(movements[randomMovementIndex], movementButtons[randomMovementIndex]);
    }
    
    void UpdateSimilarity()
    {
        float baseSimilarity = 99.9f;
        float variation = Random.Range(-0.1f, 0.1f);
        float similarity = baseSimilarity + variation;
        similarityText.text = similarity.ToString("F1") + "%";
    }
    
    void UpdateUI()
    {
        speciesSequenceText.text = "ATCGATCG";
        colorSequenceText.text = "CCTTAAGG";
        sizeSequenceText.text = "GGAATTCC";
        moveSequenceText.text = "CGATCGAT";
        
        sizeSlider.value = currentSize;
        sizeValueText.text = currentSize.ToString("F1");
        
        UpdateSimilarity();
    }
    
    void AnimateAvatar()
    {
        if (currentAvatar == null) return;
        
        animationTimer += Time.deltaTime;
        
        switch(movementPattern)
        {
            case "float":
                currentAvatar.transform.localPosition = originalAvatarPosition + Vector3.up * Mathf.Sin(animationTimer) * 0.5f;
                currentAvatar.transform.Rotate(0, 10 * Time.deltaTime, 0);
                break;
                
            case "swim":
                currentAvatar.transform.localPosition = originalAvatarPosition + Vector3.right * Mathf.Sin(animationTimer * 0.5f) * 2f;
                currentAvatar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(animationTimer * 0.5f) * 30f);
                break;
                
            case "bounce":
                currentAvatar.transform.localPosition = originalAvatarPosition + Vector3.up * Mathf.Abs(Mathf.Sin(animationTimer * 2f)) * 1.5f;
                break;
                
            case "spiral":
                Vector3 spiralPos = originalAvatarPosition;
                spiralPos.x += Mathf.Cos(animationTimer) * 1.5f;
                spiralPos.y += Mathf.Sin(animationTimer) * 1.5f;
                currentAvatar.transform.localPosition = spiralPos;
                currentAvatar.transform.Rotate(0, 20 * Time.deltaTime, 0);
                break;
        }
    }
    
    void AnimateDNAParticles()
    {
        for (int i = 0; i < dnaParticles.Count; i++)
        {
            if (dnaParticles[i] != null)
            {
                dnaParticles[i].transform.Rotate(0, 10 * Time.deltaTime, 0);
                
                Vector3 pos = dnaParticles[i].transform.localPosition;
                pos.y += Mathf.Sin(animationTimer + i * 0.5f) * 0.02f;
                pos.x += Mathf.Cos(animationTimer * 0.7f + i) * 0.01f;
                dnaParticles[i].transform.localPosition = pos;
            }
        }
    }
    
    void ResetButtonColors(Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            button.GetComponent<Image>().color = Color.white;
        }
    }
    
    // Public methods to show/hide the minigame
    public void ShowMinigame()
    {
        avatarCreatorCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f; // Pause main game
    }
    
    public void CloseMinigame()
    {
        avatarCreatorCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f; // Resume main game
    }
    
    // Method to get current avatar data (for saving/integration with main game)
    public AvatarData GetCurrentAvatarData()
    {
        return new AvatarData
        {
            species = currentSpecies,
            size = currentSize,
            color = currentColor,
            movementPattern = movementPattern
        };
    }
    
    [System.Serializable]
    public class SpeciesData
    {
        public string name;
        public Color baseColor;
        
        public SpeciesData(string name, Color baseColor)
        {
            this.name = name;
            this.baseColor = baseColor;
        }
    }
    
    [System.Serializable]
    public class AvatarData
    {
        public string species;
        public float size;
        public Color color;
        public string movementPattern;
    }
}