using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
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

    void Awake()
    {
        EnsureReferences();

        if (tmpCanvasGroup != null)
            tmpCanvasGroup.alpha = 0f;

        if (closeBtn != null)
        {
            closeBtn.gameObject.SetActive(false);
            closeBtn.onClick.AddListener(Deactivate);
        }

        // Using Vector3.Scale is independent of UI Anchors layout
        if (bodyTransform != null)
            bodyTransform.localScale = new Vector3(0f, 1f, 1f);

        gameObject.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (parentCanvasGroup == null)
            parentCanvasGroup = GetComponent<CanvasGroup>();

        if (tmpCanvasGroup == null && noteText != null)
        {
            tmpCanvasGroup = noteText.GetComponent<CanvasGroup>();
            if (tmpCanvasGroup == null)
                tmpCanvasGroup = noteText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Activate(string fact)
    {
        if (string.IsNullOrEmpty(fact))
            fact = "";

        gameObject.SetActive(true);
        
        if (noteText != null)
            noteText.text = fact;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ActivateSequence());
    }

    public void Deactivate()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(DeactivateSequence());
    }

    private IEnumerator ActivateSequence()
    {
        if (closeBtn != null) closeBtn.gameObject.SetActive(false);

        parentCanvasGroup.interactable = false;
        parentCanvasGroup.blocksRaycasts = false;

        parentCanvasGroup.alpha = 0f;
        if (tmpCanvasGroup != null) tmpCanvasGroup.alpha = 0f;
        if (bodyTransform != null) bodyTransform.localScale = new Vector3(0f, 1f, 1f);

        yield return StartCoroutine(Fade(parentCanvasGroup, 0f, 1f));
        yield return StartCoroutine(ResizeBody(0f, 1f));
        
        if (tmpCanvasGroup != null)
            yield return StartCoroutine(Fade(tmpCanvasGroup, 0f, 1f));

        parentCanvasGroup.interactable = true;
        parentCanvasGroup.blocksRaycasts = true;

        if (closeBtn != null) closeBtn.gameObject.SetActive(true);
        currentAnimation = null;
    }

    private IEnumerator DeactivateSequence()
    {
        if (closeBtn != null) closeBtn.gameObject.SetActive(false);

        parentCanvasGroup.interactable = false;
        parentCanvasGroup.blocksRaycasts = false;

        if (tmpCanvasGroup != null)
            yield return StartCoroutine(Fade(tmpCanvasGroup, 1f, 0f));
            
        yield return StartCoroutine(ResizeBody(1f, 0f));
        yield return StartCoroutine(Fade(parentCanvasGroup, 1f, 0f));

        gameObject.SetActive(false);
        currentAnimation = null;
    }

    // Combined Fade Text and Fade Parent into one reusable method
    private IEnumerator Fade(CanvasGroup targetGroup, float start, float end)
    {
        if (targetGroup == null) yield break;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            targetGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        targetGroup.alpha = end;
    }

    private IEnumerator ResizeBody(float start, float end)
    {
        if (bodyTransform == null) yield break;
        float time = 0f;

        while (time < resizeDuration)
        {
            time += Time.deltaTime;
            float t = time / resizeDuration;

            float currentX = Mathf.Lerp(start, end, t);
            bodyTransform.localScale = new Vector3(currentX, 1f, 1f);

            yield return null;
        }

        bodyTransform.localScale = new Vector3(end, 1f, 1f);
    }
}