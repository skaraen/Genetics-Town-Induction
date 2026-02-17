using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop onto UI image component to make it change colors, looping through specified sequence of colors.
/// Cycle: color -> inBetween -> color -> inBetween -> ... -> waitBeforeRestart -> repeat
/// When showStartEndLabels is enabled:
/// Start (white) -> inBetween -> color -> ... -> color -> inBetween -> End (white) -> waitBeforeRestart -> repeat
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageColorCycler : MonoBehaviour
{
    private enum State
    {
        ShowingStart,
        ShowingColor,
        ShowingInBetween,
        ShowingEnd,
        WaitingToRestart
    }

    [Tooltip("Optional text label displayed over the image (e.g. for start/end labels)")]
    [SerializeField] private TextMeshProUGUI label;

    private List<Color> colorSequence = new List<Color>();
    private float periodToDisplayColor = 1f;
    private float periodInBetween = 0f;
    private float waitBeforeRestart = 0f;
    private Color inBetweenColor;
    private Color? waitingToRestartColor = null;
    private bool showStartEndLabels = false;

    private Image image;
    private int currentIndex = 0;
    private float elapsedTime = 0f;
    private State currentState = State.ShowingColor;
    private bool transitionToEnd = false;

    void Start()
    {
        image = GetComponent<Image>();
        SetLabel("");

        if (colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
    }

    private void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    /// <summary>
    /// Configure and start the color cycling sequence.
    /// </summary>
    /// <param name="colors">List of colors to cycle through</param>
    /// <param name="periodToDisplayColor">Time in seconds to display each color</param>
    /// <param name="periodInBetween">Time in seconds to display the in-between color</param>
    /// <param name="inBetweenColor">Color to show between each main color</param>
    /// <param name="waitBeforeRestartingCycle">Time to wait after completing the sequence before restarting</param>
    /// <param name="waitingToRestartColor">Color to show while waiting to restart (defaults to inBetweenColor if null)</param>
    /// <param name="showStartEndLabels">When true, shows "Start" and "End" labels with a white circle at the beginning and end of each cycle</param>
    public void SetColorSequence(List<Color> colors, float periodToDisplayColor, float periodInBetween, Color inBetweenColor, float waitBeforeRestartingCycle = 0f, Color? waitingToRestartColor = null, bool showStartEndLabels = false)
    {
        this.colorSequence = colors;
        this.periodToDisplayColor = periodToDisplayColor;
        this.periodInBetween = periodInBetween;
        this.inBetweenColor = inBetweenColor;
        this.waitBeforeRestart = waitBeforeRestartingCycle;
        this.waitingToRestartColor = waitingToRestartColor;
        this.showStartEndLabels = showStartEndLabels;
        this.transitionToEnd = false;

        currentIndex = 0;
        elapsedTime = 0f;

        if (showStartEndLabels && colorSequence.Count > 0)
        {
            currentState = State.ShowingStart;
            if (image != null) image.color = Color.white;
            SetLabel("Start");
        }
        else
        {
            currentState = State.ShowingColor;
            SetLabel("");
            if (image != null)
            {
                image.color = colorSequence.Count > 0 ? colorSequence[0] : inBetweenColor;
            }
        }
    }

    void Update()
    {
        if (colorSequence.Count == 0) return;

        elapsedTime += Time.deltaTime;

        switch (currentState)
        {
            case State.ShowingStart:
                if (elapsedTime >= periodToDisplayColor)
                {
                    elapsedTime = 0f;
                    // Transition through in-between to the first color
                    currentIndex = -1; // Will be incremented to 0 in ShowingInBetween
                    currentState = State.ShowingInBetween;
                    image.color = inBetweenColor;
                    SetLabel("");
                }
                break;

            case State.ShowingColor:
                if (elapsedTime >= periodToDisplayColor)
                {
                    elapsedTime = 0f;

                    // Check if this was the last color
                    if (currentIndex >= colorSequence.Count - 1)
                    {
                        if (showStartEndLabels)
                        {
                            // Transition through in-between to End
                            transitionToEnd = true;
                            currentState = State.ShowingInBetween;
                            image.color = inBetweenColor;
                        }
                        else if (waitBeforeRestart > 0f)
                        {
                            currentState = State.WaitingToRestart;
                            image.color = waitingToRestartColor ?? inBetweenColor;
                        }
                        else
                        {
                            // No wait, show in-between then restart
                            currentState = State.ShowingInBetween;
                            image.color = inBetweenColor;
                        }
                    }
                    else
                    {
                        // More colors to show, go to in-between state
                        currentState = State.ShowingInBetween;
                        image.color = inBetweenColor;
                    }
                }
                break;

            case State.ShowingInBetween:
                if (elapsedTime >= periodInBetween)
                {
                    elapsedTime = 0f;

                    if (transitionToEnd)
                    {
                        transitionToEnd = false;
                        currentState = State.ShowingEnd;
                        image.color = Color.white;
                        SetLabel("End");
                    }
                    else
                    {
                        // Move to next color (wrap around if at end)
                        currentIndex = (currentIndex + 1) % colorSequence.Count;
                        currentState = State.ShowingColor;
                        image.color = colorSequence[currentIndex];
                    }
                }
                break;

            case State.ShowingEnd:
                if (elapsedTime >= periodToDisplayColor)
                {
                    elapsedTime = 0f;
                    SetLabel("");

                    if (waitBeforeRestart > 0f)
                    {
                        currentState = State.WaitingToRestart;
                        image.color = inBetweenColor;
                    }
                    else
                    {
                        // No wait, restart immediately with Start
                        currentIndex = 0;
                        currentState = State.ShowingStart;
                        image.color = Color.white;
                        SetLabel("Start");
                    }
                }
                break;

            case State.WaitingToRestart:
                if (elapsedTime >= waitBeforeRestart)
                {
                    elapsedTime = 0f;
                    currentIndex = 0;

                    if (showStartEndLabels)
                    {
                        currentState = State.ShowingStart;
                        image.color = Color.white;
                        SetLabel("Start");
                    }
                    else
                    {
                        currentState = State.ShowingColor;
                        image.color = colorSequence[0];
                    }
                }
                break;
        }
    }
}
