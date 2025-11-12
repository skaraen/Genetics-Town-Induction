using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;  // <-- Add this if missing

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject spawnerObj;
    [SerializeField] GameObject narrator;
    [SerializeField] Canvas dialogueCanvas;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] float planeDistance = 5f;

    [SerializeField] Button memoryButton;
    [SerializeField] Button glitchButton; // <-- Add your Glitch button reference

    private PlayerSpawner spawner;
    DialogueController dialogueController;
    Animator narratorAnimator;

    void OnEnable()
    {
        if (dialogueBox != null)
        {
            dialogueController = dialogueBox.GetComponent<DialogueController>();
            dialogueController.OnDialogueEnded += HandleDialogueEnded;
        }

        Camera renderCam = null;
        var brain = FindObjectOfType<CinemachineBrain>();
        if (brain) renderCam = brain.GetComponent<Camera>();
        if (!renderCam) renderCam = Camera.main;
        if (!renderCam)
        {
            Debug.LogError("No render Camera found (no CinemachineBrain and no Camera.main).");
            return;
        }

        spawner = spawnerObj.GetComponent<PlayerSpawner>();

        spawner.OnSpawnComplete += TriggerWelcome;
        spawner.OnInfected += TriggerPanic;

        if (narratorAnimator == null)
            narratorAnimator = narrator.GetComponent<Animator>();

        memoryButton.onClick.AddListener(OnMemoryButtonClicked);
        glitchButton.onClick.AddListener(OnGlitchButtonClicked); // <-- Hook it here
    }

    void OnDisable()
    {
        spawner.OnSpawnComplete -= TriggerWelcome;
        spawner.OnInfected -= TriggerPanic;

        if (dialogueController != null)
            dialogueController.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueEnded()
    {
        narratorAnimator.SetTrigger("ExitTrigger");
    }

    void TriggerWelcome()
    {
        narrator.GetComponent<ReactionChooser>().SetReaction("happy");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-first-welcome");
    }

    void TriggerPanic()
    {
        narrator.GetComponent<ReactionChooser>().SetReaction("shock");
        narratorAnimator.SetTrigger("EntryTrigger");
        dialogueController.StartSequence("narrator-glitch-panic");
    }

    void OnMemoryButtonClicked()
    {
        SceneManager.LoadScene("MemoryGame", LoadSceneMode.Additive);
    }

    void OnGlitchButtonClicked()
    {
        Debug.Log("Glitch button clicked → starting infection!");
        if (spawner != null)
            spawner.StartInfection();
        else
            Debug.LogError("Spawner reference missing!");
    }
}
