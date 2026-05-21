using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button closeBtn;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite musicOnSprite; 
    [SerializeField] private Button musicToggle;

    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    [System.Serializable]
    public struct EnvParams
    {
        public bool isMusicOn;
    }

    private EnvParams envParams;
    private AudioSource musicSource;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        closeBtn.onClick.AddListener(FadeOut);

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        envParams = new EnvParams();
        musicSource = FindAnyObjectByType<AudioSource>();
    }

    public void Activate(string animationName, string fact)
    {
        gameObject.SetActive(true);
        Init();
        FadeIn();
    }

    private void Init()
    {
       envParams.isMusicOn = musicSource != null && musicSource.isPlaying;
       SetIcons();
    }

    private void SetIcons()
    {
        musicToggle.GetComponent<Image>().sprite = envParams.isMusicOn ? musicOnSprite : musicOffSprite;
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
