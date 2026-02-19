using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class GeneTickerItem : MonoBehaviour
{
    float tickerWidth;
    float pixelsPerSecond;
    RectTransform rt;
    bool isActive = false;

    public float GetXPostition { get { return rt.anchoredPosition.x; } }
    public float GetWidth { get { return rt.rect.width; } }

    private Dictionary<string, GameObject> prefabMap;
    const string set = "ATGC";

    public void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        prefabMap = new Dictionary<string, GameObject>();
        LoadPrefabs();
    }

    void LoadPrefabs()
    {
        foreach (char c in set)
        {
            prefabMap[c + "_L"] = Resources.Load<GameObject>("Prefabs/" + c + "_L");
        }
    }

    public void Create(float tickerWidth, float pixelsPerSecond)
    {
        this.pixelsPerSecond = pixelsPerSecond;
        this.tickerWidth = tickerWidth;
    }

    void EnableText()
    {
        isActive = true;
        // rt.anchoredPosition = Vector3.zero;
    }

    void DisableText()
    {
        isActive = false;
        // rt.anchoredPosition = new Vector3(-tickerWidth - GetWidth + 10, 0);
    }
 
    public void UpdateText(string text)
    {
        foreach (char c in text)
        {
            Instantiate(prefabMap[c + "_L"], rt.transform);
        }
        EnableText();
    }

    public void UpdateDisabledText(string text)
    {
        DisableText();
    }

    // Update is called once per frame
    void Update()
    {
        if (isActive)
        {
            rt.anchoredPosition += Vector2.left * pixelsPerSecond * Time.deltaTime;

            if (GetXPostition < 0 - tickerWidth - GetWidth)
            {
                rt.anchoredPosition = Vector3.zero;
            }
        }
    }
}
