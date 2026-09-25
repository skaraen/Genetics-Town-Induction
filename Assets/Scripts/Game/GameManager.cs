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
    [SerializeField] Button CreditsButton;

    [Header("Scene Audio")]
    [SerializeField] private AudioClip worldMusic;
    [SerializeField] private AudioClip glitchMusic;

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
        if (spawner == null)
        {
            Debug.LogError("[GameManager] No PlayerSpawner found.");
            return;
        }

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

        CreditsButton.gameObject.SetActive(false);
        CreditsButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("CreditsScene");
        });

        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    void OnDisable()
    {
        dialogueController.OnDialogueEnded -= HandleDialogueEnd;
        if (spawner != null)
        {
            spawner.OnSpawnComplete -= TriggerWelcome;
            spawner.OnInfected -= TriggerPanic;
            spawner.OnCured -= AcknowledgeCure;
        }

        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    void TriggerWelcome()
    {
        // In multiplayer, PlayerSpawner fires OnSpawnComplete for each player.
        // Without this guard, the dialogue would restart with each spawn, causing
        // EntryTrigger to be set multiple times and confusing the animator.
        if (hasTriggeredWelcome)
            return;
        hasTriggeredWelcome = true;
        Debug.Log("Triggered welcom");

        MusicManager.Instance.PlayMusic(worldMusic, 1f);
        narrator.GetComponent<ReactionChooser>().SetReaction("happy");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-first-welcome");
    }

    void TriggerPanic()
    {
        MusicManager.Instance.PlayMusic(glitchMusic, 1f);
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
        CreditsButton.gameObject.SetActive(true);
        MusicManager.Instance.PlayMusic(worldMusic, 1f);
    }

    void HandleSceneUnloaded(Scene scene)
    {
        if (scene.name == "ASOGame" || scene.name == "Part2Scene")
            MusicManager.Instance.PlayMusic(worldMusic, 1f);
    }
}
