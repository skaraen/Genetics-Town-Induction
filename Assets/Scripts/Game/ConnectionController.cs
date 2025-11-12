using System.Collections;
using System.Data.SqlTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionController : MonoBehaviour
{
    private MemoryFormationGame gameManager;
    private NeuronController neuron1;
    private NeuronController neuron2;
    private int strength = 1;
    private bool isComplete = false;
    private bool isColorMatch = false;

    [Header("Components")]
    public Image connectionImage;
    public TextMeshProUGUI strengthText;
    public Button connectionButton;
    public GameObject completeEffect;

    public NeuronController Neuron1 => neuron1;
    public NeuronController Neuron2 => neuron2;
    public bool IsComplete => isComplete;

    public void Initialize(MemoryFormationGame manager, NeuronController n1, NeuronController n2)
    {
        gameManager = manager;
        neuron1 = n1;
        neuron2 = n2;

        SetupVisualConnection();
        UpdateVisuals();

        connectionButton.onClick.AddListener(() => gameManager.StrengthenConnection(this));

        if (completeEffect != null)
            completeEffect.SetActive(false);
    }

    void SetupVisualConnection()
    {
        // Calculate position and rotation between neurons
        Vector3 pos1 = neuron1.transform.position;
        Vector3 pos2 = neuron2.transform.position;

        Vector3 midPoint = (pos1 + pos2) / 2f;
        transform.position = midPoint;

        Vector3 direction = pos2 - pos1;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Set length
        float distance = Vector3.Distance(pos1, pos2);
        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(distance, 8f);
    }

    public void SetColorMatch(bool match)
    {
        isColorMatch = match;
        UpdateVisuals();
    }

    public void Strengthen()
    {
        strength++;
        UpdateVisuals();

        if (strength >= 5 && !isComplete)
        {
            isComplete = true;
            if (completeEffect != null)
                completeEffect.SetActive(true);
        }
    }

    void UpdateVisuals()
    {
        strengthText.text = strength.ToString();

        // Color based on strength and correctness
        if (isComplete)
        {
            connectionImage.color = isColorMatch ? Color.green : new Color(0.5f, 0.8f, 0.5f);
        }
        else if (strength >= 3)
        {
            connectionImage.color = isColorMatch ? Color.yellow : new Color(0.8f, 0.8f, 0.5f);
        }
        else
        {
            connectionImage.color = isColorMatch ? new Color(1f, 0.6f, 0.8f) : Color.gray;
        }

        // Thickness based on strength
        RectTransform rect = GetComponent<RectTransform>();
        float thickness = Mathf.Min(8f + strength * 2f, 16f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, thickness);
    }

    public void Celebrate()
    {
        StartCoroutine(CelebrationPulse());
    }

    IEnumerator CelebrationPulse()
    {
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.2f);
            transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.2f);
        }
    }
}