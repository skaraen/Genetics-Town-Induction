using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LandingPageManager : MonoBehaviour
{
    [Header("Main Canvas")]
    public Canvas landingCanvas;
    public Camera uiCamera;
    
    [Header("Hero Section")]
    public GameObject heroSection;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI dnaSequenceText;
    public Button enterLandButton;
    public Button learnMoreButton;
    
    [Header("Feature Cards")]
    public Transform featureContainer;
    public GameObject featureCardPrefab;
    
    [Header("Info Section")]
    public GameObject infoSection;
    public ScrollRect infoScrollRect;
    public Transform infoCardsContainer;
    public GameObject infoCardPrefab;
    public TextMeshProUGUI sectionTitleText;
    public TextMeshProUGUI sectionSubtitleText;
    
    [Header("Background Effects")]
    public Transform dnaParticleContainer;
    public GameObject dnaParticlePrefab;
    public ParticleSystem backgroundParticles;
    
    [Header("Audio")]
    public AudioSource backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioClip ambientSound;
    
    [Header("Animation Settings")]
    public float titleGlowSpeed = 2f;
    public float dnaFloatSpeed = 20f;
    public float featureHoverHeight = 10f;
    
    // Internal state
    private List<GameObject> floatingDNA = new List<GameObject>();
    private List<Transform> featureCards = new List<Transform>();
    private List<Transform> infoCards = new List<Transform>();
    private bool isInfoSectionVisible = false;
    private float animationTimer = 0f;
    private Color originalTitleColor;
    
    // Feature data
    private FeatureData[] features = new FeatureData[]
    {
        new FeatureData("🔬", "Genetic Design", "Create avatars by selecting and modifying real DNA sequences"),
        new FeatureData("🌊", "Species Variety", "Choose from fireflies, jellyfish, electric eels, whales, and bats"),
        new FeatureData("🧠", "Memory Formation", "Interact with others to create lasting neural connections"),
        new FeatureData("🦠", "Crisis Response", "Work together to cure genetic bugs and save your avatars")
    };
    
    // Info card data
    private InfoCardData[] infoCards_Data = new InfoCardData[]
    {
        new InfoCardData("🧬 Step 1: DNA Selection", "Begin by selecting random DNA sequences that determine your avatar's species. Each sequence codes for unique traits like bioluminescence in fireflies or echolocation in bats. Learn how just 0.1% genetic difference creates incredible diversity."),
        new InfoCardData("🎨 Step 2: Trait Customization", "Fine-tune your avatar by adjusting gene expression levels. Turn genes on or off, modify protein production, and see real-time changes in your avatar's appearance and abilities. Discover how genetic regulation works in living organisms."),
        new InfoCardData("🌍 Step 3: World Exploration", "Enter a shared virtual environment where your genetic creation comes to life. Visit themed areas like the DNA-to-RNA library, protein factory, and medicine restaurant. Each location teaches different aspects of molecular biology."),
        new InfoCardData("🤝 Step 4: Social Interaction", "When avatars meet, they form memories through neural connection games. Connect neurons to strengthen synapses and learn how memories are physically formed in the brain through repeated neural firing patterns."),
        new InfoCardData("🚨 Step 5: Collaborative Crisis", "Face a genetic virus that invades all avatars! Work together to identify the common viral sequence and develop ASO treatments. Experience how scientists research disease mechanisms and develop genetic therapies."),
        new InfoCardData("🏆 Step 6: Scientific Victory", "Successfully cure the genetic bug through teamwork and scientific thinking. Learn about complementary base pairing, antisense oligonucleotides, and how modern genetic medicine works to treat diseases.")
    };

    void Start()
    {
        InitializeUI();
        CreateFeatureCards();
        CreateInfoCards();
        StartBackgroundEffects();
        
        originalTitleColor = titleText.color;
        
        // Initially hide info section
        infoSection.SetActive(false);
    }
    
    void Update()
    {
        animationTimer += Time.deltaTime;
        AnimateTitleGlow();
        AnimateFloatingDNA();
    }
    
    void InitializeUI()
    {
        // Set up button listeners
        enterLandButton.onClick.AddListener(StartExperience);
        learnMoreButton.onClick.AddListener(ShowInfoSection);
        
        // Set up initial text
        titleText.text = "🧬 The Land";
        subtitleText.text = "A virtual world where genetic code creates life. Design your avatar through DNA, explore with others, and discover the building blocks of existence.";
        dnaSequenceText.text = "A T C G • A T C G • A T C G";
        
        sectionTitleText.text = "How The Land Works";
        sectionSubtitleText.text = "Experience genetics education through immersive avatar creation and collaborative problem-solving";
        
        // Add button hover effects
        AddButtonHoverEffect(enterLandButton);
        AddButtonHoverEffect(learnMoreButton);
    }
    
    void CreateFeatureCards()
    {
        foreach (FeatureData feature in features)
        {
            GameObject card = Instantiate(featureCardPrefab, featureContainer);
            SetupFeatureCard(card, feature);
            featureCards.Add(card.transform);
        }
    }
    
    void SetupFeatureCard(GameObject card, FeatureData data)
    {
        // Find child components (assuming the prefab has these)
        TextMeshProUGUI iconText = card.transform.Find("Icon").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI titleText = card.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = card.transform.Find("Description").GetComponent<TextMeshProUGUI>();
        
        iconText.text = data.icon;
        titleText.text = data.title;
        descText.text = data.description;
        
        // Add hover animation
        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(() => OnFeatureCardClick(data));
            AddCardHoverEffect(card.transform);
        }
    }
    
    void CreateInfoCards()
    {
        foreach (InfoCardData info in infoCards_Data)
        {
            GameObject card = Instantiate(infoCardPrefab, infoCardsContainer);
            SetupInfoCard(card, info);
            infoCards.Add(card.transform);
        }
    }
    
    void SetupInfoCard(GameObject card, InfoCardData data)
    {
        TextMeshProUGUI titleText = card.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = card.transform.Find("Description").GetComponent<TextMeshProUGUI>();
        
        titleText.text = data.title;
        descText.text = data.description;
        
        // Add hover animation
        AddCardHoverEffect(card.transform);
    }
    
    void StartBackgroundEffects()
    {
        // Start floating DNA particles
        InvokeRepeating(nameof(CreateFloatingDNA), 0f, 3f);
        
        // Start background particle system
        if (backgroundParticles != null)
        {
            backgroundParticles.Play();
        }
        
        // Start ambient sound
        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }
    
    void CreateFloatingDNA()
    {
        if (dnaParticlePrefab != null && dnaParticleContainer != null)
        {
            GameObject dnaParticle = Instantiate(dnaParticlePrefab, dnaParticleContainer);
            
            // Random DNA sequence
            string[] sequences = {"ATCGATCG", "GGCCTTAA", "TTAACCGG", "CCGGAATT", "AATTCCGG"};
            string randomSequence = sequences[Random.Range(0, sequences.Length)];
            
            TextMeshProUGUI dnaText = dnaParticle.GetComponent<TextMeshProUGUI>();
            if (dnaText != null)
            {
                dnaText.text = randomSequence;
                dnaText.color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.15f));
            }
            
            // Random starting position
            RectTransform rectTransform = dnaParticle.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(Random.Range(-Screen.width, Screen.width), -Screen.height);
            
            // Add to list and start animation
            floatingDNA.Add(dnaParticle);
            StartCoroutine(AnimateDNAParticle(dnaParticle));
        }
    }
    
    IEnumerator AnimateDNAParticle(GameObject particle)
    {
        RectTransform rectTransform = particle.GetComponent<RectTransform>();
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x + Random.Range(-200f, 200f), Screen.height + 100f);
        
        float duration = Random.Range(15f, 25f);
        float elapsed = 0f;
        
        while (elapsed < duration && particle != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Move upward with slight horizontal drift
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            
            // Rotate
            rectTransform.Rotate(0, 0, 30f * Time.deltaTime);
            
            yield return null;
        }
        
        // Clean up
        if (particle != null)
        {
            floatingDNA.Remove(particle);
            Destroy(particle);
        }
    }
    
    void AnimateTitleGlow()
    {
        // Create a glowing effect by modifying the title color
        float glow = Mathf.Sin(animationTimer * titleGlowSpeed) * 0.3f + 0.7f;
        titleText.color = Color.Lerp(originalTitleColor, Color.white, glow * 0.3f);
        
        // Add subtle scale animation
        float scale = 1f + Mathf.Sin(animationTimer * titleGlowSpeed * 0.5f) * 0.02f;
        titleText.transform.localScale = Vector3.one * scale;
    }
    
    void AnimateFloatingDNA()
    {
        // Additional floating animation for background DNA
        foreach (GameObject dna in floatingDNA)
        {
            if (dna != null)
            {
                RectTransform rt = dna.GetComponent<RectTransform>();
                Vector2 pos = rt.anchoredPosition;
                pos.x += Mathf.Sin(animationTimer + rt.GetInstanceID() * 0.01f) * 0.5f;
                rt.anchoredPosition = pos;
            }
        }
    }
    
    void AddButtonHoverEffect(Button button)
    {
        // Create hover effect using event triggers
        var eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        
        // Hover enter
        var hoverEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        hoverEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        hoverEnter.callback.AddListener((data) => {
            StartCoroutine(ScaleButton(button.transform, Vector3.one * 1.05f, 0.1f));
        });
        eventTrigger.triggers.Add(hoverEnter);
        
        // Hover exit
        var hoverExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        hoverExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        hoverExit.callback.AddListener((data) => {
            StartCoroutine(ScaleButton(button.transform, Vector3.one, 0.1f));
        });
        eventTrigger.triggers.Add(hoverExit);
    }
    
    void AddCardHoverEffect(Transform card)
    {
        var eventTrigger = card.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        
        // Hover enter
        var hoverEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        hoverEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        hoverEnter.callback.AddListener((data) => {
            StartCoroutine(MoveCard(card, Vector3.up * featureHoverHeight, 0.2f));
        });
        eventTrigger.triggers.Add(hoverEnter);
        
        // Hover exit
        var hoverExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        hoverExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        hoverExit.callback.AddListener((data) => {
            StartCoroutine(MoveCard(card, Vector3.zero, 0.2f));
        });
        eventTrigger.triggers.Add(hoverExit);
    }
    
    IEnumerator ScaleButton(Transform button, Vector3 targetScale, float duration)
    {
        Vector3 startScale = button.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            button.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        
        button.localScale = targetScale;
    }
    
    IEnumerator MoveCard(Transform card, Vector3 targetOffset, float duration)
    {
        Vector3 startPos = card.localPosition;
        Vector3 targetPos = startPos + targetOffset;
        
        // Remove any existing offset first
        if (targetOffset == Vector3.zero)
        {
            targetPos = new Vector3(startPos.x, startPos.y - featureHoverHeight, startPos.z);
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            card.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        
        card.localPosition = targetPos;
    }
    
    // Button click handlers
    void StartExperience()
    {
        PlayButtonSound();
        
        // You can either switch to avatar creator scene or show the minigame
        // Option 1: Load avatar creator scene
        // SceneManager.LoadScene("AvatarCreatorScene");
        
        // Option 2: Show avatar creator minigame
        GeneticAvatarCreator avatarCreator = FindObjectOfType<GeneticAvatarCreator>();
        if (avatarCreator != null)
        {
            landingCanvas.gameObject.SetActive(false);
            avatarCreator.ShowMinigame();
        }
        
        // Option 3: Load main game scene
        // SceneManager.LoadScene("MainGameScene");
    }
    
    void ShowInfoSection()
    {
        PlayButtonSound();
        
        if (!isInfoSectionVisible)
        {
            StartCoroutine(SlideToInfoSection());
        }
        else
        {
            StartCoroutine(SlideToHeroSection());
        }
        
        isInfoSectionVisible = !isInfoSectionVisible;
    }
    
    IEnumerator SlideToInfoSection()
    {
        // Animate the canvas or scroll view to show info section
        heroSection.SetActive(false);
        infoSection.SetActive(true);
        
        // Animate info cards appearing
        for (int i = 0; i < infoCards.Count; i++)
        {
            StartCoroutine(AnimateCardAppear(infoCards[i], i * 0.1f));
        }
        
        yield return null;
    }
    
    IEnumerator SlideToHeroSection()
    {
        infoSection.SetActive(false);
        heroSection.SetActive(true);
        yield return null;
    }
    
    IEnumerator AnimateCardAppear(Transform card, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        
        card.localScale = startScale;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Ease out animation
            progress = 1f - Mathf.Pow(1f - progress, 3f);
            
            card.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }
        
        card.localScale = targetScale;
    }
    
    void OnFeatureCardClick(FeatureData feature)
    {
        PlayButtonSound();
        Debug.Log($"Feature clicked: {feature.title}");
        
        // You could show a detailed popup about this feature
        // ShowFeatureDetail(feature);
    }
    
    void PlayButtonSound()
    {
        if (buttonClickSound != null && backgroundMusic != null)
        {
            backgroundMusic.PlayOneShot(buttonClickSound);
        }
    }
    
    // Public methods for external control
    public void ShowLandingPage()
    {
        landingCanvas.gameObject.SetActive(true);
        heroSection.SetActive(true);
        infoSection.SetActive(false);
        isInfoSectionVisible = false;
    }
    
    public void HideLandingPage()
    {
        landingCanvas.gameObject.SetActive(false);
    }
    
    // Data structures
    [System.Serializable]
    public class FeatureData
    {
        public string icon;
        public string title;
        public string description;
        
        public FeatureData(string icon, string title, string description)
        {
            this.icon = icon;
            this.title = title;
            this.description = description;
        }
    }
    
    [System.Serializable]
    public class InfoCardData
    {
        public string title;
        public string description;
        
        public InfoCardData(string title, string description)
        {
            this.title = title;
            this.description = description;
        }
    }
    
    void OnDestroy()
    {
        // Clean up floating DNA objects
        foreach (GameObject dna in floatingDNA)
        {
            if (dna != null) Destroy(dna);
        }
        floatingDNA.Clear();
    }
}