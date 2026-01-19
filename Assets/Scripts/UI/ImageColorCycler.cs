using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop onto UI image component to make it change colors, looping through specified sequence of colors,
/// waiting for a period of time at each color
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageColorCycler : MonoBehaviour
{
    private List<Color> colorSequence = new List<Color>();
    private float period = 1f;
    private float waitBeforeRestart = 0f;
    private Color? inBetweenCycleColor = null;
    
    private Image image;
    private int currentIndex = 0;
    private float elapsedTime = 0f;
    private bool isWaitingToRestart = false;

    void Start()
    {
        image = GetComponent<Image>();
        
        if (colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
    }

    /// <summary>
    /// Make the image color change, sequentially changing through all colors in colors, 
    /// and waiting for period seconds at each color
    /// </summary>
    /// <param name="colors">List of colors to cycle through</param>
    /// <param name="period">Time in seconds to wait at each color</param>
    /// <param name="waitBeforeRestartingCycle">If reached the end of the colors, how long to wait before starting the loop again</param>
    public void SetColorSequence(List<Color> colors, float period, float waitBeforeRestartingCycle = 0f, Color? inBetweenCycleColor = null)
    {
        this.colorSequence = colors;
        this.period = period;
        this.waitBeforeRestart = waitBeforeRestartingCycle;
        this.inBetweenCycleColor = inBetweenCycleColor;
        currentIndex = 0;
        elapsedTime = 0f;
        isWaitingToRestart = false;
        
        if (image != null && colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
    }

    void Update()
    {
        if (colorSequence.Count == 0) return;

        elapsedTime += Time.deltaTime;

        if (isWaitingToRestart)
        {
            // Waiting at the end of the cycle before restarting
            if (elapsedTime >= waitBeforeRestart)
            {
                elapsedTime = 0f;
                currentIndex = 0;
                isWaitingToRestart = false;
                image.color = colorSequence[currentIndex];
            }
        }
        else if (elapsedTime >= period)
        {
            elapsedTime = 0f;
            currentIndex++;
            
            // Check if we've reached the end of the sequence
            if (currentIndex >= colorSequence.Count)
            {
                if (waitBeforeRestart > 0f)
                {
                    // Enter wait state
                    currentIndex = colorSequence.Count - 1;
                    isWaitingToRestart = true;
                    
                    // Set to inBetweenCycleColor if provided, otherwise stay at last color
                    if (inBetweenCycleColor.HasValue)
                    {
                        image.color = inBetweenCycleColor.Value;
                    }
                }
                else
                {
                    // No wait, immediately restart
                    currentIndex = 0;
                    image.color = colorSequence[currentIndex];
                }
            }
            else
            {
                image.color = colorSequence[currentIndex];
            }
        }
    }
}
