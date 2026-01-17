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
    
    private Image image;
    private int currentIndex = 0;
    private float elapsedTime = 0f;

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
    /// <param name="colors"></param>
    /// <param name="period"></param>
    public void SetColorSequence(List<Color> colors, float period)
    {
        this.colorSequence = colors;
        this.period = period;
        currentIndex = 0;
        elapsedTime = 0f;
        
        if (image != null && colorSequence.Count > 0)
        {
            image.color = colorSequence[0];
        }
    }

    void Update()
    {
        if (colorSequence.Count == 0) return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= period)
        {
            elapsedTime = 0f;
            currentIndex = (currentIndex + 1) % colorSequence.Count;
            image.color = colorSequence[currentIndex];
        }
    }
}
