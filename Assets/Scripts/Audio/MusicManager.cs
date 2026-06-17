using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private float defaultFadeDuration = 1.5f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.mute = !AudioSettingsState.MusicOn;
    }

    private void OnEnable()
    {
        AudioSettingsState.OnSettingsChanged += ApplySettings;
    }

    private void OnDisable()
    {
        AudioSettingsState.OnSettingsChanged -= ApplySettings;
    }

    private void ApplySettings()
    {
        if (audioSource == null)
            return;

        audioSource.mute = !AudioSettingsState.MusicOn;
    }

    public void PlayMusic(AudioClip clip)
    {
        PlayMusic(clip, defaultFadeDuration);
    }

    public void PlayMusic(AudioClip clip, float fadeDuration)
    {
        if (clip == null)
            return;

        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(SwitchMusicRoutine(clip, fadeDuration / 2f));
    }

    public void FadeOutAndStop()
    {
        FadeOutAndStop(defaultFadeDuration);
    }

    public void FadeOutAndStop(float fadeDuration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOutRoutine(fadeDuration));
    }

    private IEnumerator SwitchMusicRoutine(AudioClip newClip, float duration)
    {
        while (audioSource.isPlaying && audioSource.volume > 0f)
        {
            audioSource.volume -= Time.deltaTime / duration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.mute = !AudioSettingsState.MusicOn;
        audioSource.Play();

        while (audioSource.volume < 1f)
        {
            audioSource.volume += Time.deltaTime / duration;
            yield return null;
        }

        audioSource.volume = 1f;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        fadeRoutine = null;
    }
}