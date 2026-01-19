using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class CharColorTuple{
    public char character;
    public Color color;
}

/// <summary>
/// Handles all the UI logic and transitions for part 2 of the game
/// </summary>
public class Part2GameManager : MonoBehaviour
{
    [Tooltip("Prefab with a toggle for selecting traits")]
    [SerializeField] private GameObject traitTogglePrefab;
    [SerializeField] private ImageColorCycler colorCycler;
    [Tooltip("Ordered list of slots that the player brings their DNA sequence to")]
    [SerializeField] private List<SlotHandler> OrderedSequenceSlots; 
    [Tooltip("The four initial DNA components that are draggable by the player")]
    [SerializeField] private List<DraggableItem> InitialDraggableDNAComponents;
    public List<CharColorTuple> nucleotideColors;


    void Start()
    {
        // initilize the colors of the initial draggable DNA components
        foreach(var item in InitialDraggableDNAComponents)
        {
            CharColorTuple tuple = nucleotideColors.Find(t => t.character == item.compType);
            if(tuple != null)
            {
                Color col = tuple.color;
                item.GetComponent<Image>().color = col;
            }
        }
        // Test sequence for now
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
        // test verification for now
        if(Input.GetKeyDown(KeyCode.V)){
            bool result = VerifySequence();
            Debug.Log($"Sequence verification result: {result}");
        }
    }

    /// <summary>
    /// Verify that the sequence placed in the OrderedSequenceSlots matches what it should be
    /// </summary>
    private bool VerifySequence(){
        string sequence = "";
        string correctSequence = "AGTCAGTCA"; // hardcoded for now
        foreach(var slot in OrderedSequenceSlots){
            if(slot.transform.childCount == 0){
                return false; // slot is empty
            }
            DraggableItem item = slot.transform.GetChild(0).GetComponent<DraggableItem>();
            sequence += item.compType;
        }
        return sequence == correctSequence;
    }

}
