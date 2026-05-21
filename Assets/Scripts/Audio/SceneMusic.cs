using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusic : MonoBehaviour
{
    private static SceneMusic instance;
    private AudioSource audioSource;
    private bool isManualDestroy = false;

    void Awake()
    {
        // // Singleton Pattern
        // if (instance != null && instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
    }

    void Start()
    {
        StartCoroutine(FadeIn(2f));
    }

    /// <summary>
    /// Call this from your Scene Transition script 
    /// instead of calling Destroy(musicObject) directly.
    /// </summary>
    public void StartFadeOutAndDestroy(float duration)
    {
        if (isManualDestroy) return;
        isManualDestroy = true;
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeIn(float duration)
    {
        float t = 0;
        audioSource.volume = 0;
        audioSource.Play();

        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, 1, t / duration);
            yield return null;
        }
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = audioSource.volume;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }

        audioSource.Stop();
        Destroy(gameObject);
    }
}