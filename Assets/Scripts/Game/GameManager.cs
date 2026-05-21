using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NUnit.Framework;  // <-- Add this if missing

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject spawnerObj;
    [SerializeField] GameObject narrator;
    [SerializeField] Canvas dialogueCanvas;
    [SerializeField] DialogueController dialogueController;
    [SerializeField] float planeDistance = 5f;
    [SerializeField] Button ASOButton;

    private PlayerSpawner spawner;
    Animator narratorAnimator;
    /// <summary>
    /// Prevents welcome dialogue from triggering multiple times in multiplayer.
    /// OnSpawnComplete fires once per player, so without this flag the dialogue
    /// would restart repeatedly, confusing the animator state machine.
    /// </summary>
    private bool hasTriggeredWelcome = false;
    private bool isPanicInProgress = false;

    void OnEnable()
    {
        dialogueController.OnDialogueEnded += HandleDialogueEnd;

        spawner = spawnerObj.GetComponent<PlayerSpawner>();

        spawner.OnSpawnComplete += TriggerWelcome;
        spawner.OnInfected += TriggerPanic;
        spawner.OnCured += AcknowledgeCure;

        if (narratorAnimator == null)
            narratorAnimator = narrator.GetComponent<Animator>();

        ASOButton.gameObject.SetActive(false);
        ASOButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ASOGame", LoadSceneMode.Additive);
        });
    }

    void OnDisable()
    {
        dialogueController.OnDialogueEnded -= HandleDialogueEnd;
        spawner.OnSpawnComplete -= TriggerWelcome;
        spawner.OnInfected -= TriggerPanic;
        spawner.OnCured -= AcknowledgeCure;

    }

    void TriggerWelcome()
    {
        // In multiplayer, PlayerSpawner fires OnSpawnComplete for each player.
        // Without this guard, the dialogue would restart with each spawn, causing
        // EntryTrigger to be set multiple times and confusing the animator.
        if (hasTriggeredWelcome)
            return;
        hasTriggeredWelcome = true;

        narrator.GetComponent<ReactionChooser>().SetReaction("happy");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-first-welcome");
    }

    void TriggerPanic()
    {
        narrator.GetComponent<ReactionChooser>().SetReaction("shock");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-glitch-panic");

        isPanicInProgress = true;
    }

    void HandleDialogueEnd()
    {
        if (isPanicInProgress)
        {
            ASOButton.gameObject.SetActive(true);
            isPanicInProgress = false;
        }
    }

    void AcknowledgeCure()
    {
        ASOButton.gameObject.SetActive(false);
    }
}
