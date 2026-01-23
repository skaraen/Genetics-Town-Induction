using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop onto UI image component to make it change colors, looping through specified sequence of colors.
/// Cycle: color -> inBetween -> color -> inBetween -> ... -> waitBeforeRestart -> repeat
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageColorCycler : MonoBehaviour
{
    private enum State
    {
        ShowingColor,
        ShowingInBetween,
        WaitingToRestart
    }

    private List<Color> colorSequence = new List<Color>();
    private float periodToDisplayColor = 1f;
    private float periodInBetween = 0f;
    private float waitBeforeRestart = 0f;
    private Color inBetweenColor;
    private Color? waitingToRestartColor = null;

    private Image image;
    private int currentIndex = 0;
    private float elapsedTime = 0f;
    private State currentState = State.ShowingColor;

    void Start()
    {
        image = GetComponent<Image>();

        if (colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
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
    public void SetColorSequence(List<Color> colors, float periodToDisplayColor, float periodInBetween, Color inBetweenColor, float waitBeforeRestartingCycle = 0f, Color? waitingToRestartColor = null)
    {
        this.colorSequence = colors;
        this.periodToDisplayColor = periodToDisplayColor;
        this.periodInBetween = periodInBetween;
        this.inBetweenColor = inBetweenColor;
        this.waitBeforeRestart = waitBeforeRestartingCycle;
        this.waitingToRestartColor = waitingToRestartColor;

        currentIndex = 0;
        elapsedTime = 0f;
        currentState = State.ShowingColor;

        if (image != null && colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
    }

    void Update()
    {
        if (colorSequence.Count == 0) return;

        elapsedTime += Time.deltaTime;

        switch (currentState)
        {
            case State.ShowingColor:
                if (elapsedTime >= periodToDisplayColor)
                {
                    elapsedTime = 0f;

                    // Check if this was the last color
                    if (currentIndex >= colorSequence.Count - 1)
                    {
                        // End of sequence - enter waiting state or restart
                        if (waitBeforeRestart > 0f)
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

                    // Move to next color (wrap around if at end)
                    currentIndex = (currentIndex + 1) % colorSequence.Count;
                    currentState = State.ShowingColor;
                    image.color = colorSequence[currentIndex];
                }
                break;

            case State.WaitingToRestart:
                if (elapsedTime >= waitBeforeRestart)
                {
                    elapsedTime = 0f;
                    currentIndex = 0;
                    currentState = State.ShowingColor;
                    image.color = colorSequence[0];
                }
                break;
        }
    }
}
