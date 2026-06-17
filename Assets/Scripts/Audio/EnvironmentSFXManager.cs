using UnityEngine;

public class EnvironmentSFXManager : MonoBehaviour
{
    public static EnvironmentSFXManager Instance { get; private set; }

    [Header("Environment Clips")]
    [SerializeField] private AudioClip[] walkingClips = new AudioClip[4];
    [SerializeField] private AudioClip windClip;
    [SerializeField] private AudioClip waterClip;

    [Header("Volumes")]
    [SerializeField] private float walkingVolume = 1f;
    [SerializeField] private float windVolume = 1f;
    [SerializeField] private float waterVolume = 1f;

    private AudioSource walkingSource;
    private AudioSource windSource;
    private AudioSource waterSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioClip defaultWalkingClip = null;

        if (walkingClips != null && walkingClips.Length > 0)
            defaultWalkingClip = walkingClips[0];

        walkingSource = CreateLoopingSource("Walking Source", defaultWalkingClip, walkingVolume);
        windSource = CreateLoopingSource("Wind Source", windClip, windVolume);
        waterSource = CreateLoopingSource("Water Source", waterClip, waterVolume);

        ApplySettings();
    }

    private void OnEnable()
    {
        AudioSettingsState.OnSettingsChanged += ApplySettings;
    }

    private void OnDisable()
    {
        AudioSettingsState.OnSettingsChanged -= ApplySettings;
    }

    private AudioSource CreateLoopingSource(string sourceName, AudioClip clip, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = Mathf.Clamp01(volume);
        source.mute = !AudioSettingsState.EnvironmentOn;

        return source;
    }

    private void ApplySettings()
    {
        bool muted = !AudioSettingsState.EnvironmentOn;

        if (walkingSource != null)
            walkingSource.mute = muted;

        if (windSource != null)
            windSource.mute = muted;

        if (waterSource != null)
            waterSource.mute = muted;
    }

    public void SetWalkingClipByAvatarType(int avatarTypeId)
    {
        if (walkingSource == null)
            return;

        if (walkingClips == null || walkingClips.Length == 0)
            return;

        // Mapping:
        // avatarTypeId 1 -> walkingClips[0]
        // avatarTypeId 2 -> walkingClips[1]
        // avatarTypeId 3 -> walkingClips[2]
        // avatarTypeId 4 -> walkingClips[3]
        int index = Mathf.Clamp(avatarTypeId - 1, 0, walkingClips.Length - 1);

        AudioClip selectedClip = walkingClips[index];

        if (selectedClip == null)
            return;

        if (walkingSource.clip == selectedClip)
            return;

        bool wasPlaying = walkingSource.isPlaying;

        walkingSource.Stop();
        walkingSource.clip = selectedClip;

        if (wasPlaying)
            walkingSource.Play();
    }

    public void SetWalkingVolume(float volume)
    {
        if (walkingSource == null)
            return;

        walkingVolume = Mathf.Clamp01(volume);
        walkingSource.volume = walkingVolume;
    }

    public void SetWindVolume(float volume)
    {
        if (windSource == null)
            return;

        windVolume = Mathf.Clamp01(volume);
        windSource.volume = windVolume;
    }

    public void SetWaterVolume(float volume)
    {
        if (waterSource == null)
            return;

        waterVolume = Mathf.Clamp01(volume);
        waterSource.volume = waterVolume;
    }

    public void StartWalking()
    {
        PlayLoop(walkingSource);
    }

    public void StopWalking()
    {
        StopLoop(walkingSource);
    }

    public void StartWind()
    {
        PlayLoop(windSource);
    }

    public void StopWind()
    {
        StopLoop(windSource);
    }

    public void StartWater()
    {
        PlayLoop(waterSource);
    }

    public void StopWater()
    {
        StopLoop(waterSource);
    }

    private void PlayLoop(AudioSource source)
    {
        if (source == null || source.clip == null)
            return;

        source.mute = !AudioSettingsState.EnvironmentOn;

        if (!source.isPlaying)
            source.Play();
    }

    private void StopLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
    }
}