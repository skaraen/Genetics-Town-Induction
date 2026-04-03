using System;
using System.Collections;
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

    [Header("Effects")]
    [Tooltip("Particle system prefab spawned at avatar body parts on trait completion. Should have Stop Action = Destroy.")]
    [SerializeField] private GameObject confettiPrefab;

    [Header("Completed Sequence Strip")]
    [Tooltip("VerticalLayoutGroup container at the bottom of the screen where completed nucleotide rows accumulate")]
    [SerializeField] private RectTransform completedSequenceStrip;
    [Tooltip("Prefab for a horizontal row that holds one completed sequence's nucleotides. Must have RectTransform, HorizontalLayoutGroup, and ContentSizeFitter.")]
    [SerializeField] private GameObject sequenceRowPrefab;

    [Header("Sequence Review Screen")]
    [Tooltip("Prefabs for A, T, G nucleotide labels inserted as start/stop codons in the review strip")]
    [SerializeField] private GameObject nucleotideAPrefab;
    [SerializeField] private GameObject nucleotideTPrefab;
    [SerializeField] private GameObject nucleotideGPrefab;
    [SerializeField] private GameObject nucleotideCPrefab;
    [Tooltip("Text prefab spawned to the left of each sequence row showing the trait name during review")]
    [SerializeField] private GameObject sequenceNameLabelPrefab;

    /// <summary>
    /// Manual Y offset adjustments for avatars that overlap with UI due to height differences.
    /// Key: avatar ID, Value: Y offset to apply.
    /// </summary>
    private static readonly Dictionary<int, float> AvatarYOffsets = new Dictionary<int, float>
    {
        { 2, -0.6f } // Sheep
    };

    private DialogueController dialogueController;
    private Button dialogueNextButton;
    private Animator narratorAnimator;
    private GameObject currentAvatar;
    private PlayerCustoms targetCustoms;

    // Maps sequence index → list of (renderer, originalMaterials) for restoration after grey-out
    private Dictionary<int, List<(Renderer renderer, Material[] origMaterials)>> originalMaterialsBySequence;

    // Index of the currently selected trait, matching sequenceBodyParts order in AvatarProfileData
    private int currentlySelectedTraitIndex = -1;

    // Maps trait index → list of nucleotide GameObjects for reordering in the review screen
    private Dictionary<int, List<GameObject>> nucleotidesByTraitIndex = new Dictionary<int, List<GameObject>>();

    // Maps trait index → the horizontal row RectTransform for that trait's completed nucleotides
    private Dictionary<int, RectTransform> sequenceRows = new Dictionary<int, RectTransform>();

    /// <summary>
    /// Tracks dialogue phase to differentiate intro vs final dialogue in HandleDialogueEnded.
    /// </summary>
    private enum DialoguePhase { Intro, SequenceReview }
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
    /// Instantiates the sequenceRowPrefab and parents it under the vertical completedSequenceStrip.
    /// </summary>
    private RectTransform CreateSequenceRow(int traitIndex)
    {
        GameObject rowObj = Instantiate(sequenceRowPrefab, completedSequenceStrip);
        rowObj.name = $"SequenceRow_{traitIndex}";

        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        sequenceRows[traitIndex] = rowRect;
        return rowRect;
    }

    /// <summary>
    /// Returns the full-length DNA sequence for a trait index from the avatar profile.
    /// Index 0 = baseSequence, 1 = subSequences[0], 2 = subSequences[1].
    /// </summary>
    private string GetFullSequence(int traitIndex)
    {
        AvatarProfileData profile = AvatarRegistry.Instance.GetProfileById(targetCustoms.avatarType);
        if (profile == null) return null;

        return traitIndex switch
        {
            0 => profile.baseSequence,
            1 => profile.subSequences.Length > 0 ? profile.subSequences[0] : null,
            2 => profile.subSequences.Length > 1 ? profile.subSequences[1] : null,
            _ => null
        };
    }

    /// <summary>
    /// Instantiates nucleotides for the portion of the full sequence beyond the gameplay length
    /// and appends them to the trait's sequence row. Returns the list of newly created GameObjects.
    /// </summary>
    private List<GameObject> ExpandSequenceRow(int traitIndex, RectTransform row)
    {
        List<GameObject> expanded = new List<GameObject>();
        string fullSequence = GetFullSequence(traitIndex);
        if (string.IsNullOrEmpty(fullSequence)) return expanded;

        int gameplayLength = OrderedSequenceSlots.Count;
        if (fullSequence.Length <= gameplayLength) return expanded;

        for (int i = gameplayLength; i < fullSequence.Length; i++)
        {
            GameObject prefab = GetNucleotidePrefab(fullSequence[i]);
            if (prefab == null) continue;
            GameObject instance = Instantiate(prefab, row);
            expanded.Add(instance);
        }
        return expanded;
    }

    /// <summary>
    /// Expands all sequence rows from their 9 gameplay nucleotides to the full sequence length.
    /// Called during the review transition after all traits are completed.
    /// Updates nucleotidesByTraitIndex to include the expanded nucleotides.
    /// </summary>
    private void ExpandAllSequenceRows()
    {
        List<(string traitName, string sequence)> traits = GetTraitSequencesFromProfile(targetCustoms.avatarType);

        foreach (var kvp in sequenceRows)
        {
            int traitIndex = kvp.Key;
            RectTransform row = kvp.Value;

            List<GameObject> expandedNucs = ExpandSequenceRow(traitIndex, row);

            if (nucleotidesByTraitIndex.TryGetValue(traitIndex, out List<GameObject> existing))
                existing.AddRange(expandedNucs);

            // Enable the background image and add padding now that the full sequence is shown
            Image rowBg = row.GetComponent<Image>();
            if (rowBg != null)
                rowBg.enabled = true;

            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            if (grid != null)
                grid.padding = new RectOffset(50, 50, 50, 50);

            // Spawn trait name label to the left of the row
            if (sequenceNameLabelPrefab != null && traitIndex < traits.Count)
                SpawnSequenceNameLabel(row, traits[traitIndex].traitName);
        }
    }

    /// <summary>
    /// Instantiates a trait name label as a child of the sequence row.
    /// The prefab must have LayoutElement (ignoreLayout), anchors at left-middle,
    /// and pivot at right-middle so it sits to the left of the row's content.
    /// </summary>
    private void SpawnSequenceNameLabel(RectTransform row, string traitName)
    {
        GameObject label = Instantiate(sequenceNameLabelPrefab, row);

        TextMeshProUGUI tmp = label.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = traitName;
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
        if (dialogueNextButton != null)
            dialogueNextButton.onClick.RemoveListener(HandleFinalDismiss);
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
            dialogueNextButton = dialogueBox.GetComponentInChildren<Button>(true);
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
        dialogueController.StartSequence("narrator-memory-intro");
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
            case DialoguePhase.SequenceReview:
                if (dialogueNextButton != null)
                {
                    dialogueNextButton.enabled = true;
                    dialogueNextButton.onClick.AddListener(HandleFinalDismiss);
                }
                break;
        }
    }

    /// <summary>
    /// One-shot click handler for the dialogue Next button after the review dialogue ends.
    /// Hides the avatar and unloads the Part2Scene.
    /// </summary>
    private void HandleFinalDismiss()
    {
        dialogueNextButton.onClick.RemoveListener(HandleFinalDismiss);
        dialogueNextButton.enabled = false;
        if (currentAvatar != null)
            Destroy(currentAvatar);
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Part2Scene");
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
    /// Called when the player submits their sequence for verification.
    /// </summary>
    public void SubmitSequence(){
        bool isCorrect = VerifySequence();
        if(isCorrect){
            int completedTraitIndex = currentlySelectedTraitIndex;
            Toggle completedToggle = currentlySelectedTraitToggle;
            RestoreBodyParts(completedTraitIndex);

            // Spawn confetti between the body parts and the camera so it renders in front of the avatar
            Vector3 bodyCenter = GetBodyPartCenter(completedTraitIndex);
            Vector3 toCamera = (Camera.main.transform.position - bodyCenter).normalized;
            Instantiate(confettiPrefab, bodyCenter + toCamera * 4f, Quaternion.identity);

            // Set interactable = false first, so when isOn changes, the visibility update shows the checkmark
            completedToggle.interactable = false;
            // Setting isOn = false triggers EnlargeToggleWhenSelected (scale down) and
            // ShowGameobjOnToggleInteractChange (show checkmark since interactable is false)
            // It also triggers ClearSequence via the onValueChanged listener
            completedToggle.isOn = false;

            bool allComplete = CheckAllSequencesCompleted();
            StartCoroutine(MoveNucleotidesToStrip(allComplete, completedTraitIndex));
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
    /// Moves each nucleotide from the sequence slots into the completed row for this trait,
    /// disabling drag behavior and lerping each to its layout position.
    /// If allComplete is true, triggers the completion flow after the last nucleotide lands.
    ///
    /// Animation uses a 4-step approach to avoid the GridLayoutGroup snapping mid-tween:
    /// 1. Temporarily parent all nucleotides to the row and rebuild layout to batch-compute target positions
    /// 2. Unparent to canvas root so the layout group can't override DOTween during animation
    /// 3. Stagger-animate each nucleotide to its pre-calculated target
    /// 4. Reparent to the row as permanent children once all animations finish
    /// </summary>
    private IEnumerator MoveNucleotidesToStrip(bool triggerCompletionAfter, int traitIndex)
    {
        // Get or create the horizontal row for this trait
        if (!sequenceRows.TryGetValue(traitIndex, out RectTransform targetRow))
            targetRow = CreateSequenceRow(traitIndex);

        // Collect nucleotides from slots before reparenting changes child counts
        List<GameObject> nucleotides = new List<GameObject>();
        foreach (var slot in OrderedSequenceSlots)
        {
            if (slot.transform.childCount > 0)
                nucleotides.Add(slot.transform.GetChild(0).gameObject);
        }

        // Disable interaction on all nucleotides
        foreach (GameObject nucleotide in nucleotides)
        {
            DraggableItem draggable = nucleotide.GetComponent<DraggableItem>();
            if (draggable != null)
                draggable.enabled = false;

            CanvasGroup cg = nucleotide.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.blocksRaycasts = false;
        }

        // Record starting positions, then temporarily parent all to the row
        Vector3[] startPositions = new Vector3[nucleotides.Count];
        Vector3[] targetPositions = new Vector3[nucleotides.Count];
        for (int i = 0; i < nucleotides.Count; i++)
        {
            startPositions[i] = nucleotides[i].transform.position;
            nucleotides[i].transform.SetParent(targetRow, worldPositionStays: true);
        }

        // Rebuild layout on both the row and the vertical container to compute target positions
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetRow);
        LayoutRebuilder.ForceRebuildLayoutImmediate(completedSequenceStrip);
        for (int i = 0; i < nucleotides.Count; i++)
            targetPositions[i] = nucleotides[i].transform.position;

        // Move nucleotides to the canvas root so the layout group can't fight DOTween
        // during animation. Existing row children stay put (left-aligned, unaffected).
        Transform animationParent = completedSequenceStrip.root;
        for (int i = 0; i < nucleotides.Count; i++)
        {
            nucleotides[i].transform.SetParent(animationParent, worldPositionStays: false);
            nucleotides[i].transform.position = startPositions[i];
            nucleotides[i].transform.SetAsLastSibling();
        }

        // Staggered animation: lerp each nucleotide to its pre-calculated target
        Tween lastTween = null;
        for (int i = 0; i < nucleotides.Count; i++)
        {
            lastTween = nucleotides[i].transform.DOMove(targetPositions[i], 0.3f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.15f);
        }

        // Wait for the final nucleotide to finish its lerp
        if (lastTween != null)
            yield return lastTween.WaitForCompletion();

        // Reparent all to the row as permanent layout children
        foreach (GameObject nucleotide in nucleotides)
            nucleotide.transform.SetParent(targetRow, worldPositionStays: true);

        // Store gameplay nucleotides for this trait (expanded later during review)
        nucleotidesByTraitIndex[traitIndex] = nucleotides;

        if (triggerCompletionAfter)
            StartCompletionFlow();
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
    /// Starts the game completion sequence with the sequence review screen.
    /// </summary>
    private void StartCompletionFlow()
    {
        StartCoroutine(ReviewTransitionRoutine());
    }

    /// <summary>
    /// Transitions from the game UI to the sequence review screen. Reorders nucleotides
    /// to biological order, inserts ATG/TAG labels, spawns trait annotations, and
    /// animates the strip to center-screen before triggering Gregor's final dialogue.
    /// </summary>
    private IEnumerator ReviewTransitionRoutine()
    {
        // Reparent strip from gameUI to canvas root so it survives the fade-out
        Transform canvasRoot = gameUI.transform.root;
        completedSequenceStrip.SetParent(canvasRoot, worldPositionStays: true);

        // Fade out and disable the game UI
        if (gameUI != null)
        {
            yield return gameUI.DOFade(0f, 1f).WaitForCompletion();
            gameUI.gameObject.SetActive(false);
        }

        // Reorder rows by biological order (trait 0, 1, 2)
        for (int t = 0; t < 3; t++)
        {
            if (sequenceRows.TryGetValue(t, out RectTransform row))
                row.SetSiblingIndex(t);
        }

        // Expand each row from 9 gameplay nucleotides to the full sequence length
        ExpandAllSequenceRows();

        // Insert ATG/TAG text labels at biologically correct positions
        InsertSequenceLabels();

        // Rebuild layout on each row and the container so positions are finalized
        foreach (var row in sequenceRows.Values)
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        LayoutRebuilder.ForceRebuildLayoutImmediate(completedSequenceStrip);

        // Animate strip to center of screen
        Vector3 worldPos = completedSequenceStrip.position;
        completedSequenceStrip.anchorMin = new Vector2(0.5f, 0.75f);
        completedSequenceStrip.anchorMax = new Vector2(0.5f, 0.75f);
        completedSequenceStrip.pivot = new Vector2(0.5f, 0.5f);
        completedSequenceStrip.position = worldPos;
        yield return completedSequenceStrip.DOAnchorPos(Vector2.zero, 1f)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        // Trigger Gregor's final dialogue; scene unloads after player dismisses via Next button
        currentDialoguePhase = DialoguePhase.SequenceReview;
        if (narrator != null && dialogueController != null)
        {
            narrator.GetComponent<ReactionChooser>().SetReaction("happy");
            narratorAnimator.SetTrigger("EntryTrigger");
            dialogueController.StartSequence("narrator-memory-final");
        }
    }

    /// <summary>
    /// Returns the nucleotide prefab corresponding to a DNA character (A, T, G, or C).
    /// </summary>
    private GameObject GetNucleotidePrefab(char c)
    {
        return c switch
        {
            'A' => nucleotideAPrefab,
            'T' => nucleotideTPrefab,
            'G' => nucleotideGPrefab,
            'C' => nucleotideCPrefab,
            _ => null
        };
    }

    /// <summary>
    /// Instantiates nucleotide prefabs for each letter in a codon (e.g. "ATG" or "TAG")
    /// into the given parent container starting at the given sibling index.
    /// Returns the updated insert index after all instantiated prefabs.
    /// </summary>
    private int InstantiateCodon(string codon, int insertIndex, RectTransform parent)
    {
        foreach (char c in codon)
        {
            GameObject prefab = GetNucleotidePrefab(c);
            if (prefab == null) continue;
            GameObject instance = Instantiate(prefab, parent);
            instance.transform.SetSiblingIndex(insertIndex);
            insertIndex++;
        }
        return insertIndex;
    }

    /// <summary>
    /// Prepends ATG or TAG codons at the start of each sequence row.
    /// ATG marks an active (enabled) gene, TAG marks an inactive (disabled) gene.
    /// </summary>
    private void InsertSequenceLabels()
    {
        // Trait 0 (base): always ATG
        if (sequenceRows.TryGetValue(0, out RectTransform row0))
            InstantiateCodon("ATG", 0, row0);

        // Trait 1 (subSeq0): ATG if enabled, TAG if disabled
        if (sequenceRows.TryGetValue(1, out RectTransform row1))
            InstantiateCodon(targetCustoms.subSeq0Enabled ? "ATG" : "TAG", 0, row1);

        // Trait 2 (subSeq1): ATG if enabled, TAG if disabled
        if (sequenceRows.TryGetValue(2, out RectTransform row2))
            InstantiateCodon(targetCustoms.subSeq1Enabled ? "ATG" : "TAG", 0, row2);
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

    /// <summary>
    /// Computes the world-space center of all renderers belonging to a trait's body parts.
    /// Used to position the confetti burst at the correct avatar location.
    /// </summary>
    private Vector3 GetBodyPartCenter(int traitIndex)
    {
        AvatarProfileData profile = currentAvatar.GetComponent<AvatarProfileData>();
        SequenceBodyPartGroup group = profile.sequenceBodyParts[traitIndex];
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (GameObject obj in group.bodyParts)
        {
            if (obj == null) continue;
            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
        }
        return bounds.center;
    }

}
