using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FunFactUI : MonoBehaviour
{
    [SerializeField] private TMP_Text factText;
    [SerializeField] private Image animImage;
    [SerializeField] private Button closeBtn;
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private Animator anim;
    private Coroutine currentFade;

    void Awake()
    {
        anim = animImage.GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
        closeBtn.onClick.AddListener(FadeOut);

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void PlayAnimationWithFact(string animationName, string fact)
    {
        anim.SetTrigger("Play" + animationName);
        factText.text = fact;
    }

    public void Activate(string animationName, string fact)
    {
        gameObject.SetActive(true);
        PlayAnimationWithFact(animationName, fact);
        FadeIn();
    }

    public void FadeIn()
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(0f, 1f));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float start, float end)
    {
        float time = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;

        if (end == 0f)
            gameObject.SetActive(false);
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
