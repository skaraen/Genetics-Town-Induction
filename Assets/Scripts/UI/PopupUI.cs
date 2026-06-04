using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PopupUI : MonoBehaviour
{
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private Button closeBtn;
    [SerializeField] private RectTransform bodyTransform;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float resizeDuration = 0.4f;

    private CanvasGroup parentCanvasGroup;
    private CanvasGroup tmpCanvasGroup;
    private Coroutine currentAnimation;
    private float fullBodyWidth;

    void Awake()
    {
        EnsureReferences();

        fullBodyWidth = bodyTransform.sizeDelta.x;

        if (fullBodyWidth <= 0f)
            fullBodyWidth = bodyTransform.rect.width;

        parentCanvasGroup.alpha = 0f;
        parentCanvasGroup.interactable = false;
        parentCanvasGroup.blocksRaycasts = false;

        tmpCanvasGroup.alpha = 0f;

        closeBtn.gameObject.SetActive(false);
        closeBtn.onClick.AddListener(Deactivate);

        SetBodyWidth(0f);

        gameObject.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (parentCanvasGroup == null)
        {
            parentCanvasGroup = GetComponent<CanvasGroup>();

            if (parentCanvasGroup == null)
                parentCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (tmpCanvasGroup == null && noteText != null)
        {
            tmpCanvasGroup = noteText.GetComponent<CanvasGroup>();

            if (tmpCanvasGroup == null)
                tmpCanvasGroup = noteText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Activate(string fact)
    {
        EnsureReferences();

        if (string.IsNullOrEmpty(fact))
            fact = "";

        gameObject.SetActive(true);
        noteText.text = fact;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ActivateSequence());
    }

    public void Deactivate()
    {
        EnsureReferences();

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(DeactivateSequence());
    }

    private IEnumerator ActivateSequence()
    {
        closeBtn.gameObject.SetActive(false);

        parentCanvasGroup.interactable = false;
        parentCanvasGroup.blocksRaycasts = false;

        parentCanvasGroup.alpha = 0f;
        tmpCanvasGroup.alpha = 0f;
        SetBodyWidth(0f);

        yield return StartCoroutine(Fade(0f, 1f));
        yield return StartCoroutine(ResizeBody(0f, fullBodyWidth));
        yield return StartCoroutine(FadeText(0f, 1f));

        parentCanvasGroup.interactable = true;
        parentCanvasGroup.blocksRaycasts = true;

        closeBtn.gameObject.SetActive(true);
        currentAnimation = null;
    }

    private IEnumerator DeactivateSequence()
    {
        closeBtn.gameObject.SetActive(false);

        parentCanvasGroup.interactable = false;
        parentCanvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(FadeText(1f, 0f));
        yield return StartCoroutine(ResizeBody(fullBodyWidth, 0f));
        yield return StartCoroutine(Fade(1f, 0f));

        gameObject.SetActive(false);
        currentAnimation = null;
    }

    private IEnumerator Fade(float start, float end)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            parentCanvasGroup.alpha = Mathf.Lerp(start, end, t);

            yield return null;
        }

        parentCanvasGroup.alpha = end;
    }

    private IEnumerator FadeText(float start, float end)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            tmpCanvasGroup.alpha = Mathf.Lerp(start, end, t);

            yield return null;
        }

        tmpCanvasGroup.alpha = end;
    }

    private IEnumerator ResizeBody(float start, float end)
    {
        float time = 0f;

        while (time < resizeDuration)
        {
            time += Time.deltaTime;
            float t = time / resizeDuration;

            SetBodyWidth(Mathf.Lerp(start, end, t));

            yield return null;
        }

        SetBodyWidth(end);
    }

    private void SetBodyWidth(float width)
    {
        bodyTransform.sizeDelta = new Vector2(width, bodyTransform.sizeDelta.y);
    }
}