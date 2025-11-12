using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MemoryFormationGame : MonoBehaviour
{
    [Header("Game Canvas")]
    public Canvas memoryGameCanvas;
    public GameObject gameContainer;
    
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public Slider progressBar;
    public GameObject successMessage;
    public Button resetButton;
    public Button closeButton;
    
    [Header("Game Board")]
    public Transform gameBoardContainer;
    public GameObject neuronPrefab;
    public Transform connectionContainer;
    public GameObject connectionPrefab;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip neuronClickSound;
    public AudioClip connectionSound;
    public AudioClip successSound;
    public AudioClip memoryPhaseSound;
    
    [Header("Game Settings")]
    public float colorDisplayTime = 3f;
    public float memoryFadeTime = 1f;
    public int requiredConnections = 3;
    public int connectionStrengthRequired = 5;
    
    // Game state
    private List<NeuronController> topNeurons = new List<NeuronController>();
    private List<NeuronController> bottomNeurons = new List<NeuronController>();
    private List<ConnectionController> connections = new List<ConnectionController>();
    private NeuronController selectedNeuron = null;
    private int completedConnections = 0;
    private bool showingColors = true;
    private bool gameComplete = false;
    
    // Colors for neurons
    private Color[] neuronColors = new Color[]
    {
        new Color(1f, 0.42f, 0.62f),    // Pink
        new Color(0.77f, 0.27f, 0.41f), // Dark Pink  
        new Color(0.97f, 0.71f, 0f),    // Orange
        new Color(1f, 0.39f, 0.28f),    // Red Orange
        new Color(1f, 0.46f, 0.46f),    // Light Red
        new Color(0.45f, 0.73f, 1f)     // Blue
    };
    
    private Color blackAndWhite = new Color(0.8f, 0.8f, 0.8f);

    void Start()
    {
        InitializeUI();
        memoryGameCanvas.gameObject.SetActive(false);
        ShowMemoryGame();
    }
    
    void InitializeUI()
    {
        titleText.text = "🧠 Memory Formation";
        subtitleText.text = "Connect neurons to form memories";
        
        resetButton.onClick.AddListener(ResetGame);
        closeButton.onClick.AddListener(CloseGame);
        
        progressBar.maxValue = requiredConnections;
        progressBar.value = 0;
        
        successMessage.SetActive(false);
    }
    
    public void ShowMemoryGame()
    {
        memoryGameCanvas.gameObject.SetActive(true);
        StartNewGame();
    }
    
    public void CloseGame()
    {
        memoryGameCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
    
    void StartNewGame()
    {
        ResetGameState();
        CreateNeurons();
        StartCoroutine(ColorMemoryPhase());
    }
    
    void ResetGameState()
    {
        // Clear existing neurons and connections
        foreach (NeuronController neuron in topNeurons)
        {
            if (neuron != null) Destroy(neuron.gameObject);
        }
        foreach (NeuronController neuron in bottomNeurons)
        {
            if (neuron != null) Destroy(neuron.gameObject);
        }
        foreach (ConnectionController connection in connections)
        {
            if (connection != null) Destroy(connection.gameObject);
        }
        
        topNeurons.Clear();
        bottomNeurons.Clear();
        connections.Clear();
        
        selectedNeuron = null;
        completedConnections = 0;
        showingColors = true;
        gameComplete = false;
        
        successMessage.SetActive(false);
        UpdateProgress();
    }
    
    void CreateNeurons()
    {
        // Create top row neurons
        for (int i = 0; i < 3; i++)
        {
            GameObject neuronObj = Instantiate(neuronPrefab, gameBoardContainer);
            NeuronController neuron = neuronObj.GetComponent<NeuronController>();
            neuron.Initialize(this, NeuronType.Top, i, neuronColors[i]);
            
            // Position the neuron
            RectTransform rect = neuronObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-150 + i * 150, 180);
            
            topNeurons.Add(neuron);
        }
        
        // Create bottom row neurons  
        for (int i = 0; i < 3; i++)
        {
            GameObject neuronObj = Instantiate(neuronPrefab, gameBoardContainer);
            NeuronController neuron = neuronObj.GetComponent<NeuronController>();
            neuron.Initialize(this, NeuronType.Bottom, i, neuronColors[i + 3]);
            
            // Position the neuron
            RectTransform rect = neuronObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-150 + i * 150, -180);
            
            bottomNeurons.Add(neuron);
        }
    }
    
    IEnumerator ColorMemoryPhase()
    {
        instructionText.text = "<b>MEMORIZE:</b> Remember the colors of each neuron!";
        
        if (audioSource && memoryPhaseSound)
            audioSource.PlayOneShot(memoryPhaseSound);
        
        // Flash all neurons to highlight the memory phase
        for (int flash = 0; flash < 3; flash++)
        {
            foreach (var neuron in topNeurons)
                neuron.SetBrightness(1.5f);
            foreach (var neuron in bottomNeurons)
                neuron.SetBrightness(1.5f);
                
            yield return new WaitForSeconds(0.3f);
            
            foreach (var neuron in topNeurons)
                neuron.SetBrightness(1f);
            foreach (var neuron in bottomNeurons)
                neuron.SetBrightness(1f);
                
            yield return new WaitForSeconds(0.3f);
        }
        
        // Show colors for the specified time
        yield return new WaitForSeconds(colorDisplayTime);
        
        // Fade to black and white
        yield return StartCoroutine(FadeToBlackAndWhite());
        
        showingColors = false;
        instructionText.text = "<b>CONNECT:</b> Click neurons to connect them. Match colors from memory!\nCreate " + requiredConnections + " strong connections (5 clicks each).";
    }
    
    IEnumerator FadeToBlackAndWhite()
    {
        float elapsed = 0f;
        
        while (elapsed < memoryFadeTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / memoryFadeTime;
            
            foreach (var neuron in topNeurons)
            {
                Color currentColor = Color.Lerp(neuron.OriginalColor, blackAndWhite, progress);
                neuron.SetColor(currentColor);
            }
            
            foreach (var neuron in bottomNeurons)
            {
                Color currentColor = Color.Lerp(neuron.OriginalColor, blackAndWhite, progress);
                neuron.SetColor(currentColor);
            }
            
            yield return null;
        }
        
        // Ensure final black and white state
        foreach (var neuron in topNeurons)
            neuron.SetColor(blackAndWhite);
        foreach (var neuron in bottomNeurons)
            neuron.SetColor(blackAndWhite);
    }
    
    public void OnNeuronClicked(NeuronController neuron)
    {
        if (showingColors || gameComplete) return;
        
        if (audioSource && neuronClickSound)
            audioSource.PlayOneShot(neuronClickSound);
        
        if (selectedNeuron == null)
        {
            // First selection
            selectedNeuron = neuron;
            neuron.SetSelected(true);
        }
        else if (selectedNeuron == neuron)
        {
            // Clicked same neuron, deselect
            selectedNeuron.SetSelected(false);
            selectedNeuron = null;
        }
        else if (selectedNeuron.Type != neuron.Type)
        {
            // Valid connection between different rows
            CreateConnection(selectedNeuron, neuron);
            selectedNeuron.SetSelected(false);
            selectedNeuron = null;
        }
        else
        {
            // Same row, switch selection
            selectedNeuron.SetSelected(false);
            selectedNeuron = neuron;
            neuron.SetSelected(true);
        }
    }
    
    void CreateConnection(NeuronController neuron1, NeuronController neuron2)
    {
        // Check if connection already exists
        ConnectionController existingConnection = FindConnection(neuron1, neuron2);
        
        if (existingConnection != null)
        {
            StrengthenConnection(existingConnection);
            return;
        }
        
        // Create new connection
        GameObject connectionObj = Instantiate(connectionPrefab, connectionContainer);
        ConnectionController connection = connectionObj.GetComponent<ConnectionController>();
        connection.Initialize(this, neuron1, neuron2);
        
        connections.Add(connection);
        
        if (audioSource && connectionSound)
            audioSource.PlayOneShot(connectionSound);
        
        // Check if this is a correct color match
        bool isCorrectMatch = IsColorMatch(neuron1, neuron2);
        connection.SetColorMatch(isCorrectMatch);
        
        if (isCorrectMatch)
        {
            // Briefly flash the original colors to show correct match
            StartCoroutine(FlashCorrectConnection(neuron1, neuron2));
        }
    }
    
    bool IsColorMatch(NeuronController neuron1, NeuronController neuron2)
    {
        // Define which colors should match (you can customize this logic)
        // For example: Pink matches with Blue, Orange matches with Red, etc.
        Dictionary<Color, Color> colorMatches = new Dictionary<Color, Color>
        {
            {neuronColors[0], neuronColors[5]}, // Pink -> Blue
            {neuronColors[1], neuronColors[4]}, // Dark Pink -> Light Red  
            {neuronColors[2], neuronColors[3]}  // Orange -> Red Orange
        };
        
        Color color1 = neuron1.OriginalColor;
        Color color2 = neuron2.OriginalColor;
        
        return (colorMatches.ContainsKey(color1) && colorMatches[color1] == color2) ||
               (colorMatches.ContainsKey(color2) && colorMatches[color2] == color1);
    }
    
    IEnumerator FlashCorrectConnection(NeuronController neuron1, NeuronController neuron2)
    {
        // Flash original colors briefly
        neuron1.SetColor(neuron1.OriginalColor);
        neuron2.SetColor(neuron2.OriginalColor);
        
        yield return new WaitForSeconds(0.5f);
        
        // Return to black and white
        neuron1.SetColor(blackAndWhite);
        neuron2.SetColor(blackAndWhite);
    }
    
    ConnectionController FindConnection(NeuronController neuron1, NeuronController neuron2)
    {
        foreach (ConnectionController connection in connections)
        {
            if ((connection.Neuron1 == neuron1 && connection.Neuron2 == neuron2) ||
                (connection.Neuron1 == neuron2 && connection.Neuron2 == neuron1))
            {
                return connection;
            }
        }
        return null;
    }
    
    public void StrengthenConnection(ConnectionController connection)
    {
        connection.Strengthen();
        
        if (connection.IsComplete)
        {
            completedConnections++;
            UpdateProgress();
            CheckWinCondition();
        }
    }
    
    void UpdateProgress()
    {
        progressBar.value = completedConnections;
        float percentage = (float)completedConnections / requiredConnections * 100f;
        progressText.text = $"Memory Formation: {percentage:F0}%";
    }
    
    void CheckWinCondition()
    {
        if (completedConnections >= requiredConnections)
        {
            gameComplete = true;
            successMessage.SetActive(true);
            
            if (audioSource && successSound)
                audioSource.PlayOneShot(successSound);
            
            StartCoroutine(CelebrationEffect());
        }
    }
    
    IEnumerator CelebrationEffect()
    {
        // Reveal all original colors during celebration
        foreach (var neuron in topNeurons)
        {
            neuron.SetColor(neuron.OriginalColor);
            neuron.SetBrightness(1.2f);
        }
        
        foreach (var neuron in bottomNeurons)
        {
            neuron.SetColor(neuron.OriginalColor);
            neuron.SetBrightness(1.2f);
        }
        
        // Animate connections
        foreach (var connection in connections)
        {
            connection.Celebrate();
        }
        
        // Scale animation for neurons
        for (int i = 0; i < topNeurons.Count; i++)
        {
            StartCoroutine(CelebrationBounce(topNeurons[i].transform, i * 0.1f));
            StartCoroutine(CelebrationBounce(bottomNeurons[i].transform, i * 0.1f + 0.05f));
        }
        
        yield return null;
    }
    
    IEnumerator CelebrationBounce(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Vector3 originalScale = target.localScale;
        
        // Bounce up
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.3f;
            target.localScale = originalScale * scale;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    public void ResetGame()
    {
        StartNewGame();
    }
}

public enum NeuronType
{
    Top,
    Bottom
}