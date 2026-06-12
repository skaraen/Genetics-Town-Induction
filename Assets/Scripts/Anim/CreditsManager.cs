using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreditsManager : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image bgImage;
    [SerializeField] private CodeTicker codeTicker;
    [SerializeField] private float fadeSpeed = 0.5f;

    [Header("Credits")]
    [SerializeField] private List<GameObject> creditObjects;
    [SerializeField] private float creditFadeDuration = 1f;
    [SerializeField] private float creditDisplayDuration = 3f;

    private bool binarySpawned;
    private bool backgroundReady;

    private void Start()
    {
        StartCoroutine(BeginCreditsSequence());
    }

    private IEnumerator BeginCreditsSequence()
    {
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeInBinaryBackground());

        // StartCoroutine(BinaryFlickerRoutine());

        yield return StartCoroutine(PlayCredits());
    }

    private IEnumerator FadeInBinaryBackground()
    {
        if (!binarySpawned)
        {
            codeTicker.SpawnStrands();
            binarySpawned = true;
        }

        while (bgImage.color.a > 0f)
        {
            Color c = bgImage.color;
            c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
            bgImage.color = c;

            yield return null;
        }

        yield return new WaitForSeconds(2f);

        codeTicker.UpdateStrands();
    }

    private IEnumerator BinaryFlickerRoutine()
    {
        while (true)
        {
            codeTicker.UpdateStrands();

            yield return new WaitForSeconds(
                Random.Range(0.75f, 1.5f)
            );
        }
    }

    private IEnumerator PlayCredits()
    {
        foreach (GameObject credit in creditObjects)
        {
            CanvasGroup cg = credit.GetComponent<CanvasGroup>();

            credit.SetActive(true);

            yield return FadeCanvasGroup(cg, 0f, 1f);
            yield return new WaitForSeconds(creditDisplayDuration);
            yield return FadeCanvasGroup(cg, 1f, 0f);

            credit.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float start,
        float end)
    {
        float elapsed = 0f;

        while (elapsed < creditFadeDuration)
        {
            elapsed += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                start,
                end,
                elapsed / creditFadeDuration);

            yield return null;
        }

        canvasGroup.alpha = end;
    }
}