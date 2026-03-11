using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class VerticalTickItem : MonoBehaviour
{
    float tickerHeight;
    float pixelsPerSecond;

    // +1 = move up, -1 = move down
    int direction = 1;

    RectTransform rt;
    TextMeshProUGUI tmp;
    bool isActive;

    Coroutine flickerCo;
    static readonly char[] FLICKER_CHARS = "01ACTG#@$%&*+-=<>[]{}|/\\".ToCharArray();
    System.Random rng = new System.Random();

    float Height => rt.rect.height;
    float Y      => rt.anchoredPosition.y;

    void Awake()
    {
        rt  = GetComponent<RectTransform>();
        tmp = GetComponent<TextMeshProUGUI>();
    }

    public void Init(float tickerHeight, float pixelsPerSecond, int direction, float x, float y)
    {
        this.tickerHeight    = tickerHeight;
        this.pixelsPerSecond = pixelsPerSecond;
        this.direction       = (direction >= 0) ? 1 : -1;

        tmp.ForceMeshUpdate();
        float ph = tmp.preferredHeight;
        if (ph > rt.sizeDelta.y)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ph);

        rt.anchoredPosition = new Vector2(x, y);
        isActive = true;
    }

    public void UpdateText(string text)
    {
        tmp.text = text;
        tmp.ForceMeshUpdate();
        float ph = tmp.preferredHeight;
        if (ph > rt.sizeDelta.y)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ph);
    }

    public void FlickerUpdateText(string text)
    {
        FlickerUpdateText(text, 3f, 10, false); 
    }

    public void ColoredFlickerUpdateText(string text)
    {
        FlickerUpdateText(text, 3f, 10,true); 
    }

    public void FlickerUpdateText(string text, float duration, int cps, bool isColor)
    {
        if (flickerCo != null) StopCoroutine(flickerCo);
        flickerCo = StartCoroutine(FlickerRoutine(text, duration, cps, isColor));
    }

    System.Collections.IEnumerator FlickerRoutine(string finalText, float duration, int cps, bool isColor)
    {
        float interval = 1f / Mathf.Max(1, cps);
        float elapsed = 0f;

        int L = Mathf.Max(1, finalText.Length);
        var scratch = new System.Text.StringBuilder(L);

        while (elapsed < duration)
        {
            scratch.Clear();
            string c;
            foreach (char ch in finalText)
            {
                // 70% random char, 30% show the real char (so it "locks in" over time)
                bool showReal = rng.NextDouble() < (elapsed / duration) * 0.6 + 0.3;
                if (showReal)
                {
                    c = ch.ToString();
                    if (isColor)
                        c = $"<color={GeneUtils.GetHexCode(ch)}>{c}</color>";
                } else
                {
                    c = FLICKER_CHARS[rng.Next(FLICKER_CHARS.Length)].ToString();
                }
                scratch.Append(c);
            }

            tmp.text = scratch.ToString();
            tmp.ForceMeshUpdate();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // Land on the final text and fix the rect height
        if (isColor)
        {
            string str = "";
            foreach (char ch in finalText)
            {
                str += $"<color={GeneUtils.GetHexCode(ch)}>{ch}</color>";
            }
            tmp.text = str;
        }
        else
            tmp.text = finalText;
        tmp.ForceMeshUpdate();
        float ph = tmp.preferredHeight;
        if (ph > rt.sizeDelta.y)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ph);

        flickerCo = null;
    }

    void Update()
    {
        if (!isActive) return;

        // Move in local UI space
        rt.anchoredPosition += (Vector2)(direction * Vector3.up * pixelsPerSecond * Time.deltaTime);

        // Compute bottom & top in local space (pivot = (0,0))
        float bottom = Y;
        float top    = bottom + Height;

        if (direction == 1)
        {
            // Moving up: when fully above the panel, wrap to below
            if (bottom >= tickerHeight)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -Height);
        }
        else
        {
            // Moving down: when fully below the panel, wrap to above
            if (top <= 0f)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, tickerHeight);
        }
    }

    // Optional helpers if you ever want to tweak at runtime:
    public void SetSpeed(float pixelsPerSecond) => this.pixelsPerSecond = pixelsPerSecond;
    public void SetDirection(int dir) => this.direction = (dir >= 0) ? 1 : -1;
    public void Pause(bool pause) => isActive = !pause;
}
