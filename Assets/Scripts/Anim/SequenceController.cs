using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SequenceController : MonoBehaviour
{
    [SerializeField] GameObject narrator;
    [SerializeField] GameObject dialogueUI;
    [SerializeField] GameObject coderTickerObj;
    [SerializeField] GameObject dnaObj;
    [SerializeField] GameObject variantObj;
    [SerializeField] GameObject problemObj;
    [SerializeField] GameObject lensObj;
    [SerializeField] Image bgImage;
    [SerializeField] Image lensImage;
    [SerializeField] Image geneImage;
    [SerializeField] private float fadeSpeed = 0.5f;

    private NarratorController narratorController;
    private DialogueController dialogueController;
    private CodeTicker codeTicker;

    bool spawnedBinary = false;
    bool spawnedGenetic = false;

    bool dnaFadedIn = false;
    bool dnaFadedOut = false;
    bool problemFadedIn = false;
    bool problemFadedOut = false;
    bool variantFadedIn = false;
    bool variantFadedOut = false;
    bool lensTiltIn = false;
    bool lensTiltOut = false;
    bool lensFadedIn = false;
    bool lensFadedOut = false;
    bool geneFadeIn = false;
    bool geneFadedOut = false;
    Image[] dnaImages;
    Image[] variantImages;
    Image[] problemImages;
    Animation lensAnim;

    bool bgImageFadedOut = false;
    bool isEntryDone = false;

    int currentStage = 0;

    void OnEnable()
    {
        narratorController = narrator.GetComponent<NarratorController>();
        codeTicker = coderTickerObj.GetComponent<CodeTicker>();

        if (dialogueUI != null)
        {
            dialogueController = dialogueUI.GetComponent<DialogueController>();
            dialogueController.SetNarrationMode(false);

            dialogueController.OnDialogueEnded += HandleSceneEnd; 
            dialogueController.OnDialogueProgress += HandleDialogueProgress;
        }

        narratorController.animationComplete.AddListener(() =>
        {
            dialogueController.SignalNarratorAnimComplete();
            if (!isEntryDone)
            {
                isEntryDone = true;
                dialogueController.StartSequence("narrator-main-intro");
            }
        });

        narratorController.TriggerEntry();

        dnaImages = dnaObj.GetComponentsInChildren<Image>(true);
        variantImages = variantObj.GetComponentsInChildren<Image>(true);
        problemImages = problemObj.GetComponentsInChildren<Image>(true);

        lensAnim = lensObj.GetComponent<Animation>();
    }

    void OnDisable()
    {
        dialogueController.OnDialogueEnded -= HandleSceneEnd;    
        dialogueController.OnDialogueProgress -= HandleDialogueProgress;  
    }

    void HandleSceneEnd()
    {
        SceneManager.LoadScene("AvatarSelection");
    }

    void BinaryFadeIn()
    {
        if (!spawnedBinary)
        {
            codeTicker.SpawnStrands();
            spawnedBinary = true;
        }

        Color c = bgImage.color;
        c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
        bgImage.color = c;

        if (Mathf.Approximately(c.a, 0f))
            bgImageFadedOut = true;
    }

    bool FadeTo(Image[] images, float targetAlpha)
    {
        bool allDone = true;

        foreach (Image g in images)
        {
            Color c = g.color;
            float newAlpha = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            c.a = newAlpha;
            g.color = c;

            if (!Mathf.Approximately(newAlpha, targetAlpha))
                allDone = false;
        }

        return allDone;
    }

    bool FadeToImage(Image img, float targetAlpha)
    {
        bool allDone = true;

        Color c = img.color;
        float newAlpha = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
        c.a = newAlpha;
        img.color = c;

        if (!Mathf.Approximately(newAlpha, targetAlpha))
            allDone = false;

        return allDone;
    }

    void HandleDialogueProgress()
    {
        currentStage++;
        switch (currentStage)
        {
            case 3:
                if (!spawnedGenetic)
                {
                    codeTicker.UpdateStrands();
                    spawnedGenetic = true;
                }
                break;
            case 4:
                narratorController.SwayFromTo("Mid", "Left");
                break;
            case 5:
                narratorController.SwayFromTo("Left", "Right");
                break;
            case 6:
                narratorController.SwayFromTo("Right", "Mid");
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentStage)
        {
            case 2:
                if (!bgImageFadedOut)
                    BinaryFadeIn();
                break;
            case 4:
                if (!dnaFadedIn)
                    dnaFadedIn = FadeTo(dnaImages, 1.0f);
                break;
            case 5:
                if (!dnaFadedOut)
                    dnaFadedOut = FadeTo(dnaImages, 0.0f);
                else
                {
                    if (!variantFadedIn)
                        variantFadedIn = FadeTo(variantImages, 1.0f);
                    else if (!problemFadedIn)
                        problemFadedIn = FadeTo(problemImages, 1.0f);
                }
                break;
            case 6:
                if (!variantFadedOut)
                    variantFadedOut = FadeTo(variantImages, 0.0f);
                if (!problemFadedOut)
                    problemFadedOut = FadeTo(problemImages, 0.0f);
                
                if (variantFadedOut && problemFadedOut)
                {
                    if (!lensTiltIn)
                    {
                        lensAnim.Play("TiltLensIn");
                        lensTiltIn = true;
                    } else if (!lensFadedIn) {
                        lensFadedIn = FadeToImage(lensImage, 1.0f);
                    } else if (!lensAnim.IsPlaying("TiltLensIn") && !geneFadeIn)
                    {
                        geneFadeIn = FadeToImage(geneImage, 1.0f);
                    }
                }
                break;
            case 7:
                if (!lensTiltOut)
                {
                    lensAnim.Play("TiltLensOut");
                    lensTiltOut = true;
                } else {
                    if (!lensFadedOut)
                        lensFadedOut = FadeToImage(lensImage, 0.0f);
                    if (!geneFadedOut)
                        geneFadedOut = FadeToImage(geneImage, 0.0f);
                }
                break;
        }
    }
}
