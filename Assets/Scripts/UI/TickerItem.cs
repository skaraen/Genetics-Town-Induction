using TMPro;
using UnityEngine;

public class TickerItem : MonoBehaviour
{
    float tickerWidth;
    float pixelsPerSecond;
    RectTransform rt;
    TextMeshProUGUI tmp;
    bool isActive = false;

    public float GetXPostition { get { return rt.anchoredPosition.x; } }
    public float GetWidth { get { return rt.rect.width; } }

    Coroutine flickerCo;
    static readonly char[] FLICKER_CHARS = "01ACTG#@$%&*+-=<>[]{}|/\\".ToCharArray();
    System.Random rng = new System.Random();

    Color enabledColor;
    // Color disabledColor = new Color(0, 255, 65);

    public void Awake()
    {
        rt = GetComponent<RectTransform>();
        tmp = GetComponent<TextMeshProUGUI>();
        enabledColor = tmp.color;
    }

    public void Create(float tickerWidth, float pixelsPerSecond)
    {
        this.pixelsPerSecond = pixelsPerSecond;
        this.tickerWidth = tickerWidth;
    }

    void EnableText()
    {
        isActive = true;
        tmp.color = enabledColor;
    }

    void DisableText()
    {
        isActive = false;
        tmp.color = Color.grey;
        rt.anchoredPosition = new Vector3(-tickerWidth - GetWidth + 10, 0);
    }
 
    public void UpdateText(string text)
    {
        tmp.text = text;
        EnableText();
        tmp.ForceMeshUpdate();
    }

    public void UpdateDisabledText(string text)
    {
        tmp.text = text;
        DisableText();
        tmp.ForceMeshUpdate();
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

    public void FlickerUpdateText(string text)
    {
        FlickerUpdateText(text, 1f, 10); 
    }

    public void FlickerUpdateText(string text, float duration, int cps)
    {
        if (flickerCo != null) StopCoroutine(flickerCo);
        flickerCo = StartCoroutine(FlickerRoutine(text, duration, cps));
    }

    System.Collections.IEnumerator FlickerRoutine(string finalText, float duration, int cps)
    {
        EnableText();
        float interval = 1f / Mathf.Max(1, cps);
        float elapsed = 0f;

        int L = Mathf.Max(1, finalText.Length);
        var scratch = new System.Text.StringBuilder(L);

        while (elapsed < duration)
        {
            scratch.Clear();
            for (int i = 0; i < L; i++)
            {
                bool showReal = rng.NextDouble() < elapsed / duration * 0.6 + 0.3;
                char c = showReal ? finalText[i] : FLICKER_CHARS[rng.Next(FLICKER_CHARS.Length)];
                scratch.Append(c);
            }

            tmp.text = scratch.ToString();
            tmp.ForceMeshUpdate();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        tmp.text = finalText;
        tmp.ForceMeshUpdate();

        flickerCo = null;
    }
}
