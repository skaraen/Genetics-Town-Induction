using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeBtn;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Music Toggle")]
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Button musicToggle;

    [Header("SFX Toggle")]
    [SerializeField] private Sprite sfxOffSprite;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Button sfxToggle;

    [Header("Environment Toggle")]
    [SerializeField] private Sprite environmentOffSprite;
    [SerializeField] private Sprite environmentOnSprite;
    [SerializeField] private Button environmentToggle;

    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    private void Awake()
    {
        canvasGroup = settingsUI.GetComponent<CanvasGroup>();

        closeBtn.onClick.AddListener(FadeOut);
        settingsButton.onClick.AddListener(Activate);

        musicToggle.onClick.AddListener(ToggleMusic);
        sfxToggle.onClick.AddListener(ToggleSfx);
        environmentToggle.onClick.AddListener(ToggleEnvironment);

        canvasGroup.alpha = 0f;
        settingsUI.SetActive(false);

        SetIcons();
    }

    private void OnEnable()
    {
        AudioSettingsState.OnSettingsChanged += SetIcons;
    }

    private void OnDisable()
    {
        AudioSettingsState.OnSettingsChanged -= SetIcons;
    }

    public void Activate()
    {
        settingsUI.SetActive(true);
        SetIcons();
        FadeIn();
    }

    private void ToggleMusic()
    {
        AudioSettingsState.MusicOn = !AudioSettingsState.MusicOn;
        UIAudioManager.Instance?.PlayButtonClick();
    }

    private void ToggleSfx()
    {
        AudioSettingsState.SfxOn = !AudioSettingsState.SfxOn;

        if (AudioSettingsState.SfxOn)
            UIAudioManager.Instance?.PlayButtonClick();
    }

    private void ToggleEnvironment()
    {
        AudioSettingsState.EnvironmentOn = !AudioSettingsState.EnvironmentOn;
        UIAudioManager.Instance?.PlayButtonClick();
    }

    private void SetIcons()
    {
        if (musicToggle != null)
        {
            Image musicImage = musicToggle.GetComponent<Image>();

            if (musicImage != null)
                musicImage.sprite = AudioSettingsState.MusicOn ? musicOnSprite : musicOffSprite;
        }

        if (sfxToggle != null)
        {
            Image sfxImage = sfxToggle.GetComponent<Image>();

            if (sfxImage != null)
                sfxImage.sprite = AudioSettingsState.SfxOn ? sfxOnSprite : sfxOffSprite;
        }

        if (environmentToggle != null)
        {
            Image environmentImage = environmentToggle.GetComponent<Image>();

            if (environmentImage != null)
                environmentImage.sprite = AudioSettingsState.EnvironmentOn ? environmentOnSprite : environmentOffSprite;
        }
    }

    public void FadeIn()
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(0f, 1f));
    }

    public void FadeOut()
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(1f, 0f));
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
        {
            settingsUI.SetActive(false);
        }
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        currentFade = null;
    }
}