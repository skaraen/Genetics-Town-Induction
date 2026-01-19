using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

/// <summary>
/// Handles all the UI logic and transitions for part 2 of the game
/// </summary>
public class Part2GameManager : MonoBehaviour
{
    [Tooltip("Prefab with a toggle for selecting traits")]
    [SerializeField] private GameObject traitTogglePrefab;
    [SerializeField] private ImageColorCycler colorCycler;
    
    // Mapping of DNA nucleotide letters to colors for visualization
    private Dictionary<char, Color> nucleotideColors = new Dictionary<char, Color>
    {
        { 'A', new Color(0.2f, 0.8f, 0.2f) },    // Green
        { 'T', new Color(0.8f, 0.2f, 0.2f) },    // Red
        { 'G', new Color(0.2f, 0.6f, 1.0f) },    // Blue
        { 'C', new Color(1.0f, 0.8f, 0.2f) }     // Yellow/Gold
    };


    void Start()
    {
        colorCycler.SetColorSequence(
            new List<Color> {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow
            },
            0.5f
        );
    }

    void Update()
    {
        
    }
}
