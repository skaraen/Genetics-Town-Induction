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
    [SerializeField] Button infoButton;
    [SerializeField] GameObject componentHandler;
    [SerializeField] GameObject dialogueBox;

    DialogueController dialogueController;

    void OnEnable()
    {
        int currId = 0;
        canvas[currId].SetActive(true);

        verifySequenceButton.onClick.AddListener(() =>
        {
            if (VerifySequence())
            {
                canvas[currId].SetActive(false);
                canvas[++currId].SetActive(true);
                dialogueController.StartSequence("narrator-aso-intro");
            }
        });
        resultSequenceText.enabled = false;

        verifyASOButton.onClick.AddListener(() => VerifyASO());
        resultASOText.enabled = false;

        if (dialogueBox != null)
        {
            dialogueController = dialogueBox.GetComponent<DialogueController>();
            if (dialogueController != null) {
                dialogueController.StartSequence("narrator-bug-intro");
            }
        }

        infoButton.onClick.AddListener(() => {dialogueController.StartSequence("narrator-aso-explain");});
    }

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
        else if (input != "ACTGCT")
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
        ComponentHandler handler = componentHandler.GetComponent<ComponentHandler>();
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

            dialogueController.StartSequence("narrator-aso-final");
            return;   
        }

        ShowError(resultASOText, message);
    }

    void ShowError(TextMeshProUGUI tmp, string error) {
        if (!tmp.enabled)
            tmp.enabled = true;
        tmp.text = error;
    }
}
