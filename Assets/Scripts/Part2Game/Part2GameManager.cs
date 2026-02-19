using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Dialogue")]
    [Tooltip("Gregor narrator instance in the scene")]
    [SerializeField] private GameObject narrator;
    [Tooltip("DialogueBox instance containing DialogueController")]
    [SerializeField] private GameObject dialogueBox;

    [Header("Avatar Display")]
    [Tooltip("Transform where the avatar will be spawned")]
    [SerializeField] private Transform avatarSpawnPoint;
    [Tooltip("CanvasGroup for the game UI, fades in after dialogue ends")]
    [SerializeField] private CanvasGroup gameUI;

    [Header("Grey-Out Effect")]
    [Tooltip("Material applied to avatar body parts that have not yet been unlocked by completing their sequence")]
    [SerializeField] private Material greyedOutMaterial;
    [SerializeField] private Button infoButton;
    [SerializeField] private GameObject funFactUI;
    [SerializeField] private string fact;

    /// <summary>
    /// Manual Y offset adjustments for avatars that overlap with UI due to height differences.
    /// Key: avatar ID, Value: Y offset to apply.
    /// </summary>
    private static readonly Dictionary<int, float> AvatarYOffsets = new Dictionary<int, float>
    {
        { 2, -0.6f } // Sheep
    };

    private DialogueController dialogueController;
    private Animator narratorAnimator;
    private GameObject currentAvatar;
    private PlayerCustoms targetCustoms;

    // Maps sequence index → list of (renderer, originalMaterials) for restoration after grey-out
    private Dictionary<int, List<(Renderer renderer, Material[] origMaterials)>> originalMaterialsBySequence;

    // Index of the currently selected trait, matching sequenceBodyParts order in AvatarProfileData
    private int currentlySelectedTraitIndex = -1;

    /// <summary>
    /// Tracks dialogue phase to differentiate intro vs final dialogue in HandleDialogueEnded.
    /// </summary>
    private enum DialoguePhase { Intro, Final }
    private DialoguePhase currentDialoguePhase = DialoguePhase.Intro;

    /// <summary>
    /// Clears the current sequence selection, resetting the sequence name to empty
    /// and stopping the color cycler from cycling.
    /// </summary>
    private void ClearSequence(){
        currentCorrectSequence = "";
        sequenceNameText.text = "";
        currentlySelectedTraitToggle = null;
        // Stop the color cycler by setting an empty color sequence
        colorCycler.SetColorSequence(colors: new List<Color>(), periodToDisplayColor: 0f, periodInBetween: 0f, inBetweenColor: Color.clear);
    }

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
            colorCycler.SetColorSequence(colors: sequenceColors, periodToDisplayColor: 1f, periodInBetween: 1, inBetweenColor: new Color(0,0,0,0f), waitBeforeRestartingCycle: 3f, waitingToRestartColor: Color.white, showStartEndLabels: true);
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

        AvatarProfileData profile = AvatarRegistry.Instance.GetProfileById(avatarId);
        
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
    /// Called once; sets up the game screen, initializing trait selection buttons
    /// </summary>
    private void SetupGameScreen(){
        void InitilizeTraitButtons(){
            List<(string traitName, string sequence)> traitSequences = GetTraitSequencesFromProfile(targetCustoms.avatarType);

            // Loop through each trait and create a toggle for it
            for (int i = 0; i < traitSequences.Count; i++)
            {
                print($"Trait: {traitSequences[i].traitName}, Sequence: {traitSequences[i].sequence}");
                // Capture variables for lambda
                int index = i;
                string name = traitSequences[i].traitName;
                string seq = traitSequences[i].sequence;

                // Spawn the trait selection toggle
                GameObject toggleObj = Instantiate(traitSelectionToggle, traitButtonParent);

                // Set the toggle text to the trait name
                TextMeshProUGUI toggleText = toggleObj.GetComponentInChildren<TextMeshProUGUI>();
                if(toggleText != null)
                {
                    toggleText.text = name;
                }

                // Add onValueChanged listener to handle toggle state changes
                Toggle toggle = toggleObj.GetComponentInChildren<Toggle>();
                if(toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => {
                        if(isOn)
                            SelectTraitButtonPressed(toggle, name, seq, index);
                        else if(currentlySelectedTraitToggle == toggle)
                            ClearSequence();
                    });
                }
                else{
                    Debug.LogWarning("Trait selection toggle is missing a Toggle component.");
                }
            }
        }
        
        InitilizeTraitButtons();
    }

    /// <summary>
    /// Public method subscribed to by unity toggles.
    /// <param name="selectedToggle">The toggle that was selected</param>
    /// <param name="traitName">The name of the trait selected</param>
    /// <param name="sequence">The DNA sequence associated with the trait</param>
    /// <param name="traitIndex">Index of this trait in sequenceBodyParts (0 = base, 1-2 = sub-sequences)</param>
    /// </summary>
    public void SelectTraitButtonPressed(Toggle selectedToggle, string traitName, string sequence, int traitIndex){
        // Restore the previously selected toggle if there is one
        if(currentlySelectedTraitToggle != null && currentlySelectedTraitToggle != selectedToggle)
        {
            currentlySelectedTraitToggle.isOn = false;
        }

        // Store the newly selected toggle and its sequence index
        currentlySelectedTraitToggle = selectedToggle;
        currentlySelectedTraitIndex = traitIndex;

        // Set up the sequence for this trait
        SetupSequence(traitName, sequence);
    }


    void Start()
    {
        targetCustoms = Part2DataHolder.TargetCustoms;
        SetupGameScreen();
        HideGameUI();
        InitializeDialogue();
        TriggerIntroDialogue();
    }

    /// <summary>
    /// Hides the game UI at start so only Gregor's intro dialogue is visible.
    /// </summary>
    private void HideGameUI()
    {
        if (gameUI == null) return;
        gameUI.alpha = 0f;
        gameUI.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        infoButton.onClick.AddListener(() => {
            FunFactUI funFactUIComp = funFactUI.GetComponent<FunFactUI>();

            funFactUIComp.Activate("Sequencing", fact);
            funFactUIComp.FadeIn();
        });
    }

    void OnDisable()
    {
        if (dialogueController != null)
            dialogueController.OnDialogueEnded -= HandleDialogueEnded;
    }

    /// <summary>
    /// Sets up dialogue system references and subscribes to dialogue end callback.
    /// </summary>
    private void InitializeDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueController = dialogueBox.GetComponent<DialogueController>();
            dialogueController.OnDialogueEnded += HandleDialogueEnded;
        }
        if (narrator != null)
        {
            narratorAnimator = narrator.GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Starts Gregor's intro dialogue explaining how to play the DNA sequence minigame.
    /// </summary>
    private void TriggerIntroDialogue()
    {
        if (narrator == null || dialogueController == null) return;

        narrator.GetComponent<ReactionChooser>().SetReaction("smile");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-part2-intro");
    }

    /// <summary>
    /// Callback when Gregor finishes speaking; handles different actions based on dialogue phase.
    /// </summary>
    private void HandleDialogueEnded()
    {
        if (narratorAnimator != null)
            narratorAnimator.SetTrigger("ExitTrigger");

        switch (currentDialoguePhase)
        {
            case DialoguePhase.Intro:
                ShowGameUI();
                break;
            case DialoguePhase.Final:
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Part2Scene");
                break;
        }
    }

    /// <summary>
    /// Spawns the avatar and fades in the game UI after Gregor's intro dialogue.
    /// </summary>
    private void ShowGameUI()
    {
        if (gameUI == null) return;

        SpawnAvatarDisplay();
        GreyOutAllBodyParts();
        gameUI.gameObject.SetActive(true);
        gameUI.DOFade(1f, 1f);
    }

    /// <summary>
    /// Loads and instantiates the fully customized avatar at the spawn point using the shared
    /// AvatarLoader utility, so that its traits can be animated once the player completes the DNA sequences.
    /// </summary>
    private void SpawnAvatarDisplay()
    {
        if (avatarSpawnPoint == null) return;

        if (currentAvatar != null)
            Destroy(currentAvatar);

        Vector3 spawnPos = avatarSpawnPoint.position;
        if (AvatarYOffsets.TryGetValue(targetCustoms.avatarType, out float yOffset))
        {
            spawnPos.y += yOffset;
        }

        currentAvatar = AvatarLoader.Load(targetCustoms, spawnPos, avatarSpawnPoint.rotation, avatarSpawnPoint);
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
            Toggle completedToggle = currentlySelectedTraitToggle;
            RestoreBodyParts(currentlySelectedTraitIndex);
            // Set interactable = false first, so when isOn changes, the visibility update shows the checkmark
            completedToggle.interactable = false;
            // Setting isOn = false triggers EnlargeToggleWhenSelected (scale down) and
            // ShowGameobjOnToggleInteractChange (show checkmark since interactable is false)
            // It also triggers ClearSequence via the onValueChanged listener
            completedToggle.isOn = false;

            if (CheckAllSequencesCompleted())
            {
                StartCompletionFlow();
                return;
            }

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

    /// <summary>
    /// Checks if all trait toggles are completed (non-interactable).
    /// </summary>
    private bool CheckAllSequencesCompleted()
    {
        foreach (Transform child in traitButtonParent)
        {
            Toggle toggle = child.GetComponentInChildren<Toggle>();
            if (toggle != null && toggle.interactable)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Starts the game completion sequence with fade-out and final dialogue.
    /// </summary>
    private void StartCompletionFlow()
    {
        StartCoroutine(CompletionRoutine());
    }

    private System.Collections.IEnumerator CompletionRoutine()
    {
        if (gameUI != null)
        {
            yield return gameUI.DOFade(0f, 1f).WaitForCompletion();
            gameUI.gameObject.SetActive(false);
        }
        TriggerFinalDialogue();
    }

    /// <summary>
    /// Triggers Gregor's congratulatory dialogue after all sequences are completed.
    /// </summary>
    private void TriggerFinalDialogue()
    {
        if (narrator == null || dialogueController == null) return;

        if (currentAvatar != null)
            currentAvatar.SetActive(false);

        currentDialoguePhase = DialoguePhase.Final;
        narrator.GetComponent<ReactionChooser>().SetReaction("happy");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-part2-final");
    }

    /// <summary>
    /// Replaces materials on all sequence-mapped body parts with greyedOutMaterial at game start,
    /// and stores the originals keyed by sequence index for later restoration.
    /// Only runs if greyedOutMaterial is assigned and the avatar has an AvatarProfileData component.
    /// </summary>
    private void GreyOutAllBodyParts()
    {
        if (currentAvatar == null || greyedOutMaterial == null) return;

        AvatarProfileData profile = currentAvatar.GetComponent<AvatarProfileData>();
        if (profile == null) return;

        originalMaterialsBySequence = new Dictionary<int, List<(Renderer, Material[])>>();

        for (int i = 0; i < profile.sequenceBodyParts.Count; i++)
        {
            SequenceBodyPartGroup group = profile.sequenceBodyParts[i];
            if (group == null) continue;

            var rendererData = new List<(Renderer, Material[])>();
            foreach (GameObject obj in group.bodyParts)
            {
                if (obj == null) continue;
                foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    rendererData.Add((r, r.materials));
                    Material[] grey = new Material[r.sharedMaterials.Length];
                    for (int j = 0; j < grey.Length; j++) grey[j] = greyedOutMaterial;
                    r.materials = grey;
                }
            }
            originalMaterialsBySequence[i] = rendererData;
        }
    }

    /// <summary>
    /// Restores original materials for the body part at the given sequence index.
    /// Called when the player successfully completes the matching DNA sequence.
    /// </summary>
    private void RestoreBodyParts(int sequenceIndex)
    {
        if (originalMaterialsBySequence == null) return;
        if (!originalMaterialsBySequence.TryGetValue(sequenceIndex, out var rendererData)) return;

        foreach (var (renderer, origMaterials) in rendererData)
        {
            if (renderer != null)
                renderer.materials = origMaterials;
        }
        originalMaterialsBySequence.Remove(sequenceIndex);
    }

}
