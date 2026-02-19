using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class FlickerComponent : MonoBehaviour
{
    RectTransform rt;
    TextMeshProUGUI tmp;

    Coroutine flickerCo;
    static readonly char[] FLICKER_CHARS = "01ACTG#@$%&*+-=<>[]{}|/".ToCharArray();
    System.Random rng = new System.Random();

    float Height => rt.rect.height;
    float Y      => rt.anchoredPosition.y;

    void Awake()
    {
        rt  = GetComponent<RectTransform>();
        tmp = GetComponent<TextMeshProUGUI>();
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
        FlickerUpdateText(text, 3f, 10); 
    }

    public void FlickerUpdateText(string text, float duration, int cps)
    {
        if (flickerCo != null) StopCoroutine(flickerCo);
        flickerCo = StartCoroutine(FlickerRoutine(text, duration, cps));
    }

    System.Collections.IEnumerator FlickerRoutine(string finalText, float duration, int cps)
    {
        float interval = 1f / Mathf.Max(1, cps);
        float elapsed = 0f;

        int L = Mathf.Max(1, finalText.Length);
        var scratch = new System.Text.StringBuilder(L);

        while (elapsed < duration)
        {
            scratch.Clear();
            for (int i = 0; i < L; i++)
            {
                // 70% random char, 30% show the real char (so it "locks in" over time)
                bool showReal = rng.NextDouble() < (elapsed / duration) * 0.6 + 0.3;
                char c = showReal ? finalText[i] : FLICKER_CHARS[rng.Next(FLICKER_CHARS.Length)];
                scratch.Append(c);
            }

            tmp.text = scratch.ToString();
            tmp.ForceMeshUpdate();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // Land on the final text and fix the rect height
        tmp.text = finalText;
        tmp.ForceMeshUpdate();
        float ph = tmp.preferredHeight;
        if (ph > rt.sizeDelta.y)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, ph);

        flickerCo = null;
    }
}
