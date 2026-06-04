using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ASOGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject[] canvas;
    [SerializeField] GameObject inputField;
    [SerializeField] TextMeshProUGUI resultSequenceText;
    [SerializeField] TextMeshProUGUI resultASOText;
    [SerializeField] Button verifySequenceButton;
    [SerializeField] Button verifyASOButton;
    [SerializeField] Button[] infoButton;
    [SerializeField] GameObject componentHandlerObj;
    [SerializeField] DialogueController dialogueUI;
    [SerializeField] GameObject funFactUI;
    [SerializeField] string[] facts;
    [SerializeField] PopupUI popupUI;
    [SerializeField] [TextArea(3, 6)] string popupNote;

    [Header("Scene Audio")]
    [SerializeField] private AudioClip bugMusic;
    [SerializeField] private AudioClip asoMusic;

    string bug;
    int currStageId;
    PlayerSpawner spawner;
    bool isGameComplete = false;

    void OnEnable()
    {
        currStageId = 0;
        canvas[currStageId].SetActive(true);

        dialogueUI.OnDialogueEnded += HandleDialogueEnd;

        spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            bug = spawner.GetBugSequence();
            Debug.Log($"[ASOGame] The bug sequence is: {bug}");
        }

        verifySequenceButton.onClick.AddListener(() =>
        {
            if (VerifySequence())
            {
                ComponentHandler componentHandler = componentHandlerObj.GetComponent<ComponentHandler>();
                componentHandler.SetBug(bug);

                canvas[currStageId].SetActive(false);
                canvas[++currStageId].SetActive(true);

                dialogueUI.StartSequence("narrator-aso-intro");
                MusicManager.Instance.PlayMusic(asoMusic, 1f);
            }
        });
        resultSequenceText.enabled = false;

        verifyASOButton.onClick.AddListener(() => VerifyASO());
        resultASOText.enabled = false;

        for (int i = 0; i < 2; i++)
        {
            int index = i;
            infoButton[index].onClick.AddListener(() => {
                FunFactUI funFactUIComp = funFactUI.GetComponent<FunFactUI>();

                string animName = (index == 0) ? "Disease" : "ASO";

                funFactUIComp.Activate(animName, facts[index]);
                funFactUIComp.FadeIn();
            });
        }
        
        popupUI.Activate(popupNote);
        MusicManager.Instance.PlayMusic(bugMusic, 1f);
    }

    /// <summary>
    /// Reads the user input box in the aso game and verifies if the sequence is correct
    /// </summary>
    /// <returns>
    /// True if the sequence is correct, false otherwise
    /// </returns>
    bool VerifySequence() {
        string raw = inputField.GetComponent<TextMeshProUGUI>().text;
        string input = Regex.Replace(raw ?? string.Empty, @"\s|\p{Cf}", "").ToUpperInvariant();
        string errorMessage;
        if (input.Length != 6)
        {
            if (input.Length == 0)
                errorMessage = "Enter a sequence to verify.";
            else
                errorMessage = "Sequence is 6 characters long.";
        }
        else if (input != bug)
        {
            errorMessage = "Sequence is wrong. Try again.";
        }
        else
        {
            return true;
        }

        ShowError(resultSequenceText, errorMessage);
        return false;
    }

    void VerifyASO()
    {
        string message;
        ComponentHandler handler = componentHandlerObj.GetComponent<ComponentHandler>();
        if (!handler.IsASOValid())
            message = "Complete the ASO to synthesize.";
        else if (!handler.IsASOCorrect())
            message = "Synthesized ASO wrong. Use the guide for reference.";
        else
        {
            if (!resultASOText.enabled)
                resultASOText.enabled = true;
            resultASOText.text = "ASO Synthesis successful.";
            resultASOText.color = Color.green;

            isGameComplete = true;
            dialogueUI.StartSequence("narrator-aso-final");
            UIAudioManager.Instance.PlayEndGame();
            return;   
        }

        ShowError(resultASOText, message);
    }

    void ShowError(TextMeshProUGUI tmp, string error) {
        if (!tmp.enabled)
            tmp.enabled = true;
        tmp.text = error;
    }

    void HandleDialogueEnd()
    {
        if (!isGameComplete) return;

        if (spawner != null)
            spawner.CureInfectionServerRpc();
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("ASOGame");
    }
}
