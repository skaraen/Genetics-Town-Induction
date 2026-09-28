using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    [SerializeField] private GameObject header;
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject narrator;
    [SerializeField] private Button nextButton;

    private DialogueLine[] dialogues;
    private TextMeshProUGUI dialogueText;

    private RectTransform headerRect;
    private RectTransform bodyRect;

    private Vector2 headerActivePos;
    private Vector2 bodyActivePos;

    private Vector2 headerInactivePos;
    private Vector2 bodyInactivePos;

    private Vector2 headerTargetPos;
    private Vector2 bodyTargetPos;

    private Vector2 headerVelocity = Vector2.zero;
    private Vector2 bodyVelocity = Vector2.zero;

    private bool isAnimating = false;
    private bool isTyping = false;
    private bool isActive = false;
    private bool isNarratorAnimating = false;
    private bool isSequenceInProgress = false;

    private int currStage = 0;
    private DialogueLine currDialogue;
    private int currIdx;
    private string visibleText, hiddenText;

    private float smoothTime = 0.2f;
    private float typeSpeed = 0.02f;
    private float typeTimer = 0f;

    public event Action OnDialogueProgress;
    public event Action OnDialogueEnded;

    Animator narratorAnimator;
    ReactionChooser reactChooser;
    private bool isNarrationMode;

    string beginTag = "<color=#00000000>";
    string endTag = "</color>";

    void Awake()
    {
        dialogueText = body.GetComponentInChildren<TextMeshProUGUI>();

        headerRect = header.GetComponent<RectTransform>();
        bodyRect = body.GetComponent<RectTransform>();

        bodyActivePos = bodyRect.anchoredPosition;
        headerActivePos = headerRect.anchoredPosition;

        bodyInactivePos = bodyActivePos + new Vector2(0, -600);
        headerInactivePos = headerActivePos + new Vector2(-600, 0);
    }

    void OnEnable()
    {
        dialogueText.text = "";

        bodyTargetPos = bodyActivePos;
        headerTargetPos = headerActivePos;

        bodyRect.anchoredPosition = bodyInactivePos;
        headerRect.anchoredPosition = headerInactivePos;

        isNarrationMode = true;

        if (narrator != null)
        {
            narratorAnimator = narrator.GetComponent<Animator>();
            reactChooser = narrator.GetComponent<ReactionChooser>();
        }

        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(NextButtonClick);
    }

    void OnDisable()
    {
        nextButton.onClick.RemoveListener(NextButtonClick);
    }

    public void SetNarrationMode(bool mode) {
        isNarrationMode = mode;
    }

    public void StartSequence(string sequence)
    {
        if (DialogueDatabase.Instance == null)
            Debug.LogError("DialogueDatabase is missing!");

        if (isNarrationMode)
        {
            UIAudioManager.Instance?.PlayNarratorEntry();

            narratorAnimator.SetTrigger("EntryTrigger"); 
        }       
        
        dialogues = DialogueDatabase.Instance.GetLines(sequence);
        isSequenceInProgress = true;
        currStage = 0;

        ProgressDialogue();
    }

    public void SignalNarratorAnimComplete()
    {
        isNarratorAnimating = false;
    }

    void ProgressDialogue()
    {   
        OnDialogueProgress?.Invoke();
        // if (isNarrationMode)
        //     reactChooser.SetReaction(currDialogue.emotion);

        if (!isActive && !isAnimating)
        {
            isAnimating = true;
        }
        else if (isTyping)
        {
            dialogueText.text = currDialogue.text;
            currIdx = currDialogue.text.Length;
            isTyping = false;
        }
        else if (isActive && !isTyping)
        {
            currStage++;

            if (currStage < dialogues.Length)
                StartTyping();
            else
                EndDialogue();
        }
    }
    void NextButtonClick()
    {
        Debug.Log("Hello Dialougue Next Click");
        if (isSequenceInProgress && (isNarrationMode || !isNarratorAnimating) && !isTyping)
        {
            ProgressDialogue();
            UIAudioManager.Instance.PlayButtonClick();
        }
    }

    void Update()
    {
        if (isAnimating)
        {
            bodyRect.anchoredPosition = Vector2.SmoothDamp(
                bodyRect.anchoredPosition,
                bodyTargetPos,
                ref bodyVelocity,
                smoothTime
            );

            headerRect.anchoredPosition = Vector2.SmoothDamp(
                headerRect.anchoredPosition,
                headerTargetPos,
                ref headerVelocity,
                smoothTime
            );

            if (Vector2.Distance(bodyRect.anchoredPosition, bodyTargetPos) < 0.01f && 
                    Vector2.Distance(headerRect.anchoredPosition, headerTargetPos) < 0.01f)
            {
                isAnimating = false;

                if (isSequenceInProgress)
                {
                    isActive = true;
                    StartTyping();
                    bodyTargetPos = bodyInactivePos;
                    headerTargetPos = headerInactivePos;
                }
                else
                {
                    bodyTargetPos = bodyActivePos;
                    headerTargetPos = headerActivePos;
                }
            }
        }

        if (isTyping)
        {
            typeTimer += Time.deltaTime;

            if (typeTimer >= typeSpeed && currIdx <= currDialogue.text.Length)
            {
                visibleText = currDialogue.text.Substring(0, currIdx);
                hiddenText = currDialogue.text.Substring(currIdx);
                dialogueText.text = visibleText + beginTag + hiddenText + endTag;
                currIdx++;
                typeTimer = 0f;
            }

            if (currIdx > currDialogue.text.Length)
            {
                isTyping = false;
                StartCoroutine(delayedButtonSetActive(nextButton, true));
            }
        }
    }

    IEnumerator delayedButtonSetActive(Button button, bool isActive)
    {
        yield return new WaitForSeconds(1.5f);

        button.gameObject.SetActive(isActive);
    }

    private void StartTyping()
    {
        UIAudioManager.Instance?.PlayNarratorSpeaking();
        
        nextButton.gameObject.SetActive(false);
        currDialogue = dialogues[currStage];
        currIdx = 0;
        dialogueText.text = "";
        isTyping = true;
        typeTimer = 0f;
    }

    private void EndDialogue()
    {
        isSequenceInProgress = false;
        isTyping = false;
        isActive = false;
        dialogueText.text = "";
        isAnimating = true;
        nextButton.gameObject.SetActive(false);

        if (isNarrationMode)
        {
            UIAudioManager.Instance?.PlayNarratorEntry();
            narratorAnimator.SetTrigger("ExitTrigger");
        }

        OnDialogueEnded?.Invoke();
    }
}
