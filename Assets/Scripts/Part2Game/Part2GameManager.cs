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

    void Start()
    {
        colorCycler.SetColorSequence(
            new System.Collections.Generic.List<Color> {
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
