using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SequenceController : MonoBehaviour
{
    [SerializeField] GameObject narrator;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] GameObject coderTickerObj;
    [SerializeField] Image dnaImage;
    [SerializeField] Image bgImage;
    [SerializeField] Button skipButton;
    [SerializeField] private float fadeSpeed = 2f;

    private NarratorController narratorController;
    private ReactionChooser reactionChooser;
    private DialogueController dialogueController;
    private CodeTicker codeTicker;

    bool spawnedBinary = false;
    bool spawnedGenetic = false;

    bool dnaFadedIn = false;
    bool dnaFadedOut = false;

    bool bgImageFadedOut = false;
    bool isEntryDone = false;

    void OnEnable()
    {
        narratorController = narrator.GetComponent<NarratorController>();
        codeTicker = coderTickerObj.GetComponent<CodeTicker>();

        if (dialogueBox != null)
        {
            dialogueController = dialogueBox.GetComponent<DialogueController>();
            dialogueController.SetNarrationMode(false);

            dialogueController.OnDialogueEnded += HandleSceneEnd;   
        }

        narratorController.animationComplete.AddListener(() =>
        {
            dialogueController.SignalNarratorAnimComplete();
            if (!isEntryDone)
            {
                isEntryDone = true;
                dialogueController.StartSequence("narrator-main-intro");
                reactionChooser.SetReaction("happy");
            }
        });

        skipButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("AvatarSelection");
        });
        reactionChooser = narrator.GetComponent<ReactionChooser>();
        narratorController.TriggerEntry();
    }

    void OnDisable()
    {
        dialogueController.OnDialogueEnded -= HandleSceneEnd;      
    }

    void HandleSceneEnd()
    {
        SceneManager.LoadScene("AvatarSelection");
    }

    // Update is called once per frame
    void Update()
    {
        if (!bgImageFadedOut && narratorController.currentStage == 1)
        {
            reactionChooser.SetReaction("smile");
            Color c = bgImage.color;
            c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
            bgImage.color = c;

            if (Mathf.Approximately(c.a, 0f))
                bgImageFadedOut = true;

            if (!spawnedBinary)
            {
                codeTicker.SpawnStrands();
                spawnedBinary = true;
            }
        }
        else if (!spawnedGenetic && narratorController.currentStage == 2)
        {
            codeTicker.UpdateStrands();
            spawnedGenetic = true;
        } else if (narratorController.currentStage == 4) {
            reactionChooser.SetReaction("happy");
        }
        // else if (!dnaFadedIn && narratorController.currentStage == 4)
        // {
        //     // Fade in
        //     Color c = dnaImage.color;
        //     c.a = Mathf.MoveTowards(c.a, 1f, fadeSpeed * Time.deltaTime);
        //     dnaImage.color = c;

        //     if (Mathf.Approximately(c.a, 1f))
        //         dnaFadedIn = true;
        // }
        // else if (!dnaFadedOut && narratorController.currentStage == 5)
        // {
        //     // Fade out
        //     Color c = dnaImage.color;
        //     c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
        //     dnaImage.color = c;

        //     if (Mathf.Approximately(c.a, 0f))
        //         dnaFadedOut = true;
        // }
    }
}
