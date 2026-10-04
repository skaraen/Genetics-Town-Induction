using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip celebrationClip;
    [SerializeField] private AudioClip endGameClip;
    [SerializeField] private AudioClip draggableDropClip;

    [Header("Narrator Sound Effects")]
    [SerializeField] private AudioClip narratorEntryClip;
    [SerializeField] private AudioClip narratorSpeakingClip;
    [SerializeField] private AudioClip bubbleClickClip;
    [Tooltip("Repeat the typing clip until the dialogue controller calls StopNarratorSpeaking.")]
    [SerializeField] private bool loopNarratorSpeaking = true;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    // Short UI effects keep using the existing AudioSource.
    private AudioSource audioSource;
    // Typing gets its own source so stopping it cannot cut off other UI sounds.
    private AudioSource narratorSpeakingSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        volume = Mathf.Clamp01(volume);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        // Created automatically; no additional Inspector reference is needed.
        GameObject speakingObject = new GameObject("NarratorSpeakingAudio");
        speakingObject.transform.SetParent(transform, false);
        narratorSpeakingSource = speakingObject.AddComponent<AudioSource>();
        narratorSpeakingSource.playOnAwake = false;
        narratorSpeakingSource.loop = loopNarratorSpeaking;
        narratorSpeakingSource.spatialBlend = 0f;
        narratorSpeakingSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        narratorSpeakingSource.pitch = audioSource.pitch;
        narratorSpeakingSource.priority = audioSource.priority;
        narratorSpeakingSource.ignoreListenerPause = audioSource.ignoreListenerPause;
        narratorSpeakingSource.volume = audioSource.volume * volume;

        ApplySettings();
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        AudioSettingsState.OnSettingsChanged += ApplySettings;
        ApplySettings();
    }

    private void OnDisable()
    {
        AudioSettingsState.OnSettingsChanged -= ApplySettings;
        StopNarratorSpeaking();
    }

    private void OnDestroy()
    {
        StopNarratorSpeaking();
        if (narratorSpeakingSource != null)
            Destroy(narratorSpeakingSource.gameObject);
        if (Instance == this)
            Instance = null;
    }

    private void ApplySettings()
    {
        bool muted = !AudioSettingsState.SfxOn;
        if (audioSource != null)
            audioSource.mute = muted;
        if (narratorSpeakingSource != null)
            narratorSpeakingSource.mute = muted;
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickClip);
    }

    public void PlayCelebration()
    {
        PlaySound(celebrationClip);
    }

    public void PlayEndGame()
    {
        PlaySound(endGameClip);
    }

    public void PlayDraggableDropClip()
    {
        PlaySound(draggableDropClip);
    }

    public void PlayNarratorEntry()
    {
        PlaySound(narratorEntryClip);
    }

    public void PlayNarratorSpeaking()
    {
        if (!isActiveAndEnabled || narratorSpeakingSource == null || narratorSpeakingClip == null)
            return;
        if (!AudioSettingsState.SfxOn)
            return;

        ApplySettings();
        narratorSpeakingSource.volume = audioSource.volume * volume;
        narratorSpeakingSource.loop = loopNarratorSpeaking;

        // Repeated calls cannot stack copies of the typing sound.
        if (narratorSpeakingSource.isPlaying && narratorSpeakingSource.clip == narratorSpeakingClip)
            return;

        narratorSpeakingSource.Stop();
        narratorSpeakingSource.clip = narratorSpeakingClip;
        narratorSpeakingSource.Play();
    }

    public void StopNarratorSpeaking()
    {
        if (narratorSpeakingSource == null) return;
        narratorSpeakingSource.Stop();
        narratorSpeakingSource.clip = null;
    }

    public void PlayBubbleClick()
    {
        PlaySound(bubbleClickClip);
    }

    public void PlaySound(AudioClip clip)
    {
        if (!isActiveAndEnabled || audioSource == null || clip == null)
            return;
        if (!AudioSettingsState.SfxOn)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (narratorSpeakingSource != null && audioSource != null)
            narratorSpeakingSource.volume = audioSource.volume * volume;
    }
}
