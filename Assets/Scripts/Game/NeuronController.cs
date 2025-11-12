using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeuronController : MonoBehaviour
{
    private MemoryFormationGame gameManager;
    private NeuronType neuronType;
    private int index;
    private Color originalColor;
    private bool isSelected = false;

    [Header("Components")]
    public Image neuronImage;
    public TextMeshProUGUI labelText;
    public Button neuronButton;
    public GameObject selectionGlow;

    public NeuronType Type => neuronType;
    public Color OriginalColor => originalColor;

    public void Initialize(MemoryFormationGame manager, NeuronType type, int idx, Color color)
    {
        gameManager = manager;
        neuronType = type;
        index = idx;
        originalColor = color;

        neuronImage.color = color;
        labelText.text = type == NeuronType.Top ? $"N{idx + 1}" : $"M{idx + 1}";

        neuronButton.onClick.AddListener(() => gameManager.OnNeuronClicked(this));

        if (selectionGlow != null)
            selectionGlow.SetActive(false);
    }

    public void SetColor(Color color)
    {
        neuronImage.color = color;
    }

    public void SetBrightness(float multiplier)
    {
        Color currentColor = neuronImage.color;
        neuronImage.color = currentColor * multiplier;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionGlow != null)
            selectionGlow.SetActive(selected);
    }
}