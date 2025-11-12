using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;

public class NarratorController : MonoBehaviour
{
    private Animator animator;
    public UnityEvent animationComplete;
    private bool isEntryComplete = false;
    private int currentSway = 0;
    public int currentStage = 0;
    private System.Random rand;

    void Start()
    {
        rand = new System.Random();
    }

    public void TriggerEntry()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("PeekTrigger");
        animator.SetTrigger("EntryTrigger");    
    }

    public void OnAnimationComplete()
    {
        animationComplete.Invoke();

        if (!isEntryComplete)
        {
            isEntryComplete = true;
        }
    }

    void Update()
    {
        if (isEntryComplete && Input.GetKeyDown(KeyCode.Space))
        {
            if (currentStage < 3)
            {
                currentSway = (currentSway + 1) % 3;
                int t = currentSway + 1;

                animator.SetTrigger("SwayTrigger" + t);
            }
            currentStage++;
        }
    }
}
