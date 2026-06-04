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

    [Header("Settings")]
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

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
        audioSource.loop = false;
        audioSource.playOnAwake = false;
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

    public void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }
}