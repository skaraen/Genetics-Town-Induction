using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Properties;
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
    [SerializeField] private ImageColorCycler colorCycler;
    [Tooltip("Ordered list of slots that the player brings their DNA sequence to")]
    [SerializeField] private List<SlotHandler> OrderedSequenceSlots; 
    [Tooltip("The four initial DNA components that are draggable by the player")]
    [SerializeField] private List<DraggableItem> InitialDraggableDNAComponents;
    public List<CharColorTuple> nucleotideColors;
    [SerializeField] private TextMeshProUGUI sequenceNameText;

    [Tooltip("Toggle to spawn in the trait select UI for the player to select which trait to edit. Must have a TMP in its children.")]
    [SerializeField] private GameObject traitSelectionToggle;
    [Tooltip("Parent transform to hold all the trait selection buttons")]
    [SerializeField] private Transform traitButtonParent;
    private Toggle currentlySelectedTraitToggle;
    private string currentCorrectSequence; // the correct sequence for the currently selected trait
    [Tooltip("Submit button to verify the entered sequence")]
    [SerializeField] private GameObject submitButton;

    /// <summary>
    /// Sets up the game manager for a specific sequence, setting the sequence name, and
    /// making the flashing circle colors match the DNA in the sequence.
    /// <param name="traitName">The name of the sequence to set up</param>
    /// <param name="correctSequence">The correct DNA sequence, such as ACTGACTGA, for verification</param>
    /// </summary>
    private void SetupSequence(string traitName, string correctSequence){
        void SetupColorCycler(){
            // Build color list from the correct sequence
            List<Color> sequenceColors = new List<Color>();
            foreach(char nucleotide in correctSequence)
            {
                CharColorTuple tuple = nucleotideColors.Find(t => t.character == nucleotide);
                if(tuple != null)
                {
                    sequenceColors.Add(tuple.color);
                }
            }
            
            // Set the color cycler to cycle through the sequence colors
            // flash white in between cycles for 2 seconds
            colorCycler.SetColorSequence(colors: sequenceColors, periodToDisplayColor: 1f, periodInBetween: 1, inBetweenColor: Color.black, waitBeforeRestartingCycle: 3f, waitingToRestartColor: Color.white);
        }

        // Store the correct sequence for verification
        this.currentCorrectSequence = correctSequence;
        SetupColorCycler();
        sequenceNameText.text = $"{traitName}"; // display trait name to player
    }

    /// <summary>
    /// Extracts all trait-sequence pairs from an avatar profile, combining the base sequence
    /// with its trait name and all subsequences with their respective trait names.
    /// </summary>
    /// <param name="avatarId">The ID of the avatar profile to extract from</param>
    /// <returns>List of tuples containing (traitName, sequence) pairs, or empty list if profile not found</returns>
    private List<(string traitName, string sequence)> GetTraitSequencesFromProfile(int avatarId)
    {

        List<(string traitName, string sequence)> traitSequences = new List<(string, string)>();

        void AddTraitSequence(string name, string seq)
        {
            // Limit to the number of sequence slots available in the UI
            traitSequences.Add((name, seq.Substring(0, OrderedSequenceSlots.Count)));
        }

        AvatarProfile profile = AvatarProfileManager.Instance.GetProfileById(avatarId);
        
        if (profile == null) return traitSequences;
        
        // Add base sequence and its trait name
        if (!string.IsNullOrEmpty(profile.baseSequenceTraitName))
        {
            AddTraitSequence(profile.baseSequenceTraitName, profile.baseSequence);
        }
        
        // Add all subsequences and their trait names
        for (int i = 0; i < profile.subSequences.Length; i++)
        {
            if (i < profile.subSequencesTraitNames.Length && !string.IsNullOrEmpty(profile.subSequencesTraitNames[i]))
            {
                AddTraitSequence(profile.subSequencesTraitNames[i], profile.subSequences[i]);
            }
        }

        return traitSequences;
    }

    /// <summary>
    /// Called once; sets up the game screen, initializing draggable DNA components and trait selection buttons
    /// </summary>
    private void SetupGameScreen(){
        void InitilizeDraggableDNAComponents()
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
        }
        
        void InitilizeTraitButtons(){
            // Extract avatarprofile data - get profile by ID (e.g., jellyfish = 1)
            int avatarId = 2; // TODO: Get this from player data or scene context
            List<(string traitName, string sequence)> traitSequences = GetTraitSequencesFromProfile(avatarId);

            // Loop through each trait and create a toggle for it
            foreach(var trait in traitSequences)
            {
                print($"Trait: {trait.traitName}, Sequence: {trait.sequence}");
                // Capture variables for lambda
                string name = trait.traitName;
                string seq = trait.sequence;
                
                // Spawn the trait selection toggle
                GameObject toggleObj = Instantiate(traitSelectionToggle, traitButtonParent);
                
                // Set the toggle text to the trait name
                TextMeshProUGUI toggleText = toggleObj.GetComponentInChildren<TextMeshProUGUI>();
                if(toggleText != null)
                {
                    toggleText.text = name;
                }
                
                // Add onValueChanged listener to call SelectTraitButtonPressed when toggled on
                Toggle toggle = toggleObj.GetComponentInChildren<Toggle>();
                if(toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => {
                        if(isOn) SelectTraitButtonPressed(toggle, name, seq);
                    });
                }
                else{
                    Debug.LogWarning("Trait selection toggle is missing a Toggle component.");
                }
            }
        }
        
        InitilizeDraggableDNAComponents();
        InitilizeTraitButtons();
    }

    /// <summary>
    /// Public method subscribed to by unity toggles.
    /// <param name="selectedToggle">The toggle that was selected</param>
    /// <param name="traitName">The name of the trait selected</param>
    /// <param name="sequence">The DNA sequence associated with the trait</param>
    /// </summary>
    public void SelectTraitButtonPressed(Toggle selectedToggle, string traitName, string sequence){
        // Restore the previously selected toggle if there is one
        if(currentlySelectedTraitToggle != null && currentlySelectedTraitToggle != selectedToggle)
        {
            currentlySelectedTraitToggle.interactable = true;
            currentlySelectedTraitToggle.isOn = false;
        }

        // Lock the newly selected toggle (can only deselect by selecting another)
        selectedToggle.interactable = false;

        // Store the newly selected toggle
        currentlySelectedTraitToggle = selectedToggle;

        // Set up the sequence for this trait
        SetupSequence(traitName, sequence);
    }


    void Start()
    {
        SetupGameScreen();
    }

    /// <summary>
    /// Subscribed to from slothandlers callOnStateChange in the inspector; Called whenever the ordered sequence state changes (item added/removed/moved)
    /// </summary>
    public void OrderedSequenceStateChanged(){
        foreach(var slot in OrderedSequenceSlots){
            if(slot.transform.childCount == 0){
                // disable submit button if any slot is empty
                submitButton.SetActive(false);
                return;
            }
        }
        // enable submit button if all slots are filled
        submitButton.SetActive(true);
    }


    /// <summary>
    /// Called when the player submits their sequence for verification
    /// </summary>
    public void SubmitSequence(){
        bool isCorrect = VerifySequence();
        if(isCorrect){
            currentlySelectedTraitToggle.interactable = false; // disable the toggle for this trait
            currentlySelectedTraitToggle.isOn = false; // deselect the toggle
            // clear the sequence slots
            foreach(var slot in OrderedSequenceSlots){
                if(slot.transform.childCount > 0){
                    Destroy(slot.transform.GetChild(0).gameObject);
                }
            }
        }
        else{
            // Shake all DNA components in slots to indicate incorrect sequence
            foreach(var slot in OrderedSequenceSlots){
                if(slot.transform.childCount > 0){
                    RectTransform childRect = slot.transform.GetChild(0).GetComponent<RectTransform>();
                    // If there's an active tween on this object, kill it and reset position
                    if(DOTween.IsTweening(childRect)){
                        DOTween.Kill(childRect);
                        childRect.localPosition = Vector3.zero; // Reset to center of slot
                    }
                    // Start the shake animation
                    childRect.DOShakePosition(0.5f, strength: 30f, vibrato: 20, randomness: 0f);
                }
            }
        }
    }

    /// <summary>
    /// Verify that the sequence placed in the OrderedSequenceSlots matches what it should be
    /// </summary>
    private bool VerifySequence(){
        string sequence = "";
        foreach(var slot in OrderedSequenceSlots){
            if(slot.transform.childCount == 0){
                return false; // slot is empty
            }
            DraggableItem item = slot.transform.GetChild(0).GetComponent<DraggableItem>();
            sequence += item.compType;
        }
        print($"sequence entered: {sequence}, correct sequence: {currentCorrectSequence}");
        return sequence == currentCorrectSequence;
    }

}
