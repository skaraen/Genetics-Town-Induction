using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    [SerializeField] private GameObject header;
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject narrator;

    private DialogueLine[] dialogues;
    private TextMeshProUGUI dialogueText;

    private Vector3 headerStartPos;
    private Vector3 bodyStartPos;
    private Vector3 headerTargetPos;
    private Vector3 bodyTargetPos;

    private Vector3 headerActivePos;
    private Vector3 bodyActivePos;
    private Vector3 headerInactivePos;
    private Vector3 bodyInactivePos;

    private Vector3 headerVelocity = Vector3.zero;
    private Vector3 bodyVelocity = Vector3.zero;

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

    void OnEnable()
    {
        dialogueText = body.GetComponentInChildren<TextMeshProUGUI>();
        dialogueText.text = "";

        bodyActivePos = body.transform.position;
        headerActivePos = header.transform.position;

        bodyInactivePos = bodyActivePos + new Vector3(0, -600, 0);
        headerInactivePos = headerActivePos + new Vector3(-600, 0, 0);

        bodyTargetPos = bodyActivePos;
        headerTargetPos = headerActivePos;

        bodyStartPos = bodyInactivePos;
        headerStartPos = headerInactivePos;

        body.transform.position = bodyStartPos;
        header.transform.position = headerStartPos;

        isNarrationMode = true;
        if (narrator != null)
        {
            narratorAnimator = narrator.GetComponent<Animator>();
            reactChooser = narrator.GetComponent<ReactionChooser>();
        }
    }

    public void SetNarrationMode(bool mode) {
        isNarrationMode = mode;
    }

    public void StartSequence(string sequence)
    {
        if (DialogueDatabase.Instance == null)
        {
            Debug.LogError("DialogueDatabase is missing!");
        } else
        {
            Debug.LogError("Oaky!");
        }
        dialogues = DialogueDatabase.Instance.GetLines(sequence);
        isSequenceInProgress = true;
        currStage = 0;
        ProgressDialogue();
        if (isNarrationMode)
            narratorAnimator.SetTrigger("EntryTrigger");
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

    void Update()
    {
        if (isSequenceInProgress && (isNarrationMode || !isNarratorAnimating) && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !isTyping) 
            ProgressDialogue();

        if (isAnimating)
        {
            body.transform.position = Vector3.SmoothDamp(
                body.transform.position, bodyTargetPos, ref bodyVelocity, smoothTime
            );

            header.transform.position = Vector3.SmoothDamp(
                header.transform.position, headerTargetPos, ref headerVelocity, smoothTime
            );

            if (Vector3.Distance(body.transform.position, bodyTargetPos) < 0.01f &&
                Vector3.Distance(header.transform.position, headerTargetPos) < 0.01f)
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
            }
        }
    }

    private void StartTyping()
    {
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

        if (isNarrationMode)
            narratorAnimator.SetTrigger("ExitTrigger");

        OnDialogueEnded?.Invoke();
    }
}
