using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private Button settingsButton;
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
        canvasGroup = settingsUI.GetComponent<CanvasGroup>();
        closeBtn.onClick.AddListener(FadeOut);
        settingsButton.onClick.AddListener(Activate);
        musicToggle.onClick.AddListener(ToggleMusic);

        canvasGroup.alpha = 0f;
        settingsUI.SetActive(false);

        envParams = new EnvParams();
        musicSource = FindAnyObjectByType<AudioSource>();
    }

    public void Activate()
    {
        settingsUI.SetActive(true);
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
        Image toggleImage = musicToggle.GetComponent<Image>();
        toggleImage.sprite = envParams.isMusicOn ? musicOnSprite : musicOffSprite;
    }

    private void ToggleMusic()
    {
        if (musicSource == null)
            return;

        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            envParams.isMusicOn = false;
        }
        else
        {
            musicSource.Play();
            envParams.isMusicOn = true;
        }

        SetIcons();
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
            settingsUI.SetActive(false);
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
