using UnityEngine;

public class Ticker : MonoBehaviour
{
    [SerializeField] float tickSpeed;
    [SerializeField] GameObject tickerItemPrefab;
    // TickerItem tickerItem;
    // void Awake()
    // {
    //     float width = GetComponent<RectTransform>().rect.width;
    //     float pixelsPerSecond = width / tickSpeed;

    //     tickerItem = tickerItemPrefab.GetComponent<TickerItem>();
    //     tickerItem.Create(width, pixelsPerSecond);
    // }

    // public void UpdateTicker(string text)
    // {
    //     tickerItem.UpdateText(text);
    // }

    // public void UpdateDisabledTicker(string text)
    // {
    //     tickerItem.UpdateDisabledText(text);
    // }

    // public void FlickerUpdateTicker(string text)
    // {
    //     tickerItem.FlickerUpdateText(text);
    // }
    GeneTickerItem tickerItem;
    void Awake()
    {
        float width = GetComponent<RectTransform>().rect.width;
        float pixelsPerSecond = width / tickSpeed;

        tickerItem = tickerItemPrefab.GetComponent<GeneTickerItem>();
        tickerItem.Create(width, pixelsPerSecond);
    }

    public void UpdateTicker(string text)
    {
        tickerItem.UpdateText(text);
    }

    public void UpdateDisabledTicker(string text)
    {
        tickerItem.UpdateDisabledText(text);
    }
}
