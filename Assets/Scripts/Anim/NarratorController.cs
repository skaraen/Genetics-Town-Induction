using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;

public class NarratorController : MonoBehaviour
{
    private Animator animator;
    public UnityEvent animationComplete;

    void OnEnable()
    {
        animator = GetComponent<Animator>();
    }

    public void TriggerEntry()
    {
        animator.SetTrigger("PeekTrigger");
        animator.SetTrigger("EntryTrigger");    
    }

    public void OnAnimationComplete()
    {
        animationComplete.Invoke();
    }

    public void SwayFromTo(string startPos, string endPos)
    {
        if (string.IsNullOrEmpty(startPos) || string.IsNullOrEmpty(endPos))
            return;
        string transitionKey = startPos[0].ToString() + endPos[0];
        
        animator.SetTrigger("Sway" + transitionKey.ToUpper() + "Trigger");
    }
}
