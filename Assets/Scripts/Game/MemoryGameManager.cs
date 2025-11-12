using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MemoryGameManager : MonoBehaviour
{
    [Header("UI elements")]
    [SerializeField] GameObject seqTickerObj;
    [SerializeField] TextMeshProUGUI nameTmp;
    [SerializeField] TextMeshProUGUI learningTmp;
    [SerializeField] TextMeshProUGUI countdownTmp;
    [SerializeField] TextMeshProUGUI resultTmp;
    [SerializeField] Image resultImg;
    [SerializeField] Transform spawnPoint;

    [Header("Neuron Buttons")]
    [SerializeField] Button[] topNeurons;
    [SerializeField] Button[] bottomNeurons;

    [Header("UI")]
    [SerializeField] GameObject dialogueBox;
    [SerializeField] private float fadeDuration = 2f;

    private GameObject currentAvatar;
    private Material[] avatarMaterials;
    private Color[] origBaseColors;
    private Color[] origEmisColors;

    const int NUM_NEURONS = 3;
    Ticker seqTicker;
    int[] topOrder = { 1, 2, 3 };
    int[] bottomOrder = { 1, 2, 3 };
    int topChosen = -1;
    int bottomChosen = -1;
    int learning = 0;
    bool isGameOver;

    Coroutine flickerCo;
    Coroutine learningCo;
    Coroutine countdownCo;
    Coroutine resultCo;
    Coroutine endCo;
    static readonly char[] FLICKER_CHARS = "01ACTG#@$%&*+-=<>[]{}|/\\".ToCharArray();
    System.Random rng = new System.Random();

    GameObject[] synapses;
    bool resultRunning = false;
    DialogueController dialogueController;

    private void OnEnable()
    {
        InitGame();
        SpawnAvatar();
        ResetNeurons();

        if (dialogueBox != null)
        {
            dialogueController = dialogueBox.GetComponent<DialogueController>();
            if (dialogueController != null) {
                dialogueController.OnDialogueEnded += HandleDialogueEnded;
                dialogueController.StartSequence("narrator-memory-intro");
            }
        }
    }

    void InitGame()
    {
        isGameOver = false;

        seqTicker = seqTickerObj.GetComponent<Ticker>();
        seqTicker.UpdateDisabledTicker("AGTAAAGGAGAAGAACTTTTCACTGGAGTTGTC");
        synapses = new GameObject[NUM_NEURONS];

        for (int i = 0; i < NUM_NEURONS; i++)
        {
            int index = i;
            synapses[index] = topNeurons[index].transform.Find("Synapse").gameObject;

            topNeurons[index].onClick.AddListener(() =>
            {
                topChosen = topOrder[index];
                ShowNeuron(topNeurons[index], "top", index, true);
                CheckCorrectness();
            });

            bottomNeurons[index].onClick.AddListener(() =>
            {
                bottomChosen = bottomOrder[index];
                ShowNeuron(bottomNeurons[index], "bottom", index, true);
                CheckCorrectness();
            });
        }
    }

    private void OnDisable()
    {
        if (dialogueController != null)
            dialogueController.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueEnded()
    {
        if (isGameOver)
            SceneManager.UnloadSceneAsync("MemoryGame");

        InitiateCountdown();
    }

    void ShowNeuron(Button btn, string type, int idx, bool isEnabled)
    {
        int c;
        if (type == "top")
        {
            if (!isEnabled)
                c = 8;
            else {
                c = topOrder[idx] + NUM_NEURONS;
                synapses[idx].SetActive(true);
            }
        }
        else
        {
            if (!isEnabled)
                c = 7;
            else
                c = bottomOrder[idx];
        }

        btn.image.sprite = Resources.Load<Sprite>("Images/Neurons/" + c);
    }

    void HideNeuron(Button btn, string type, int idx)
    {
        synapses[idx].SetActive(false);
        ShowNeuron(btn, type, idx, false);
    }

    void ResetNeurons()
    {
        ArrayUtils.ShuffleArray(topOrder);
        ArrayUtils.ShuffleArray(bottomOrder);

        for (int i = 0; i < NUM_NEURONS; i++)
        {
            ShowNeuron(topNeurons[i], "top", i, true);
            ShowNeuron(bottomNeurons[i], "bottom", i, true);
        }
    }

    void InitiateCountdown()
    {
        if (countdownCo != null) StopCoroutine(countdownCo);
        countdownCo = StartCoroutine(CountdownAfterResult());
    }

    System.Collections.IEnumerator CountdownAfterResult()
    {
        yield return new WaitWhile(() => resultRunning);

        yield return CountdownRoutine();
    }

    System.Collections.IEnumerator CountdownRoutine()
    {
        countdownTmp.gameObject.SetActive(true);
        countdownTmp.text = "Ready?";
        yield return new WaitForSecondsRealtime(1f);
        countdownTmp.text = "Here We Go!";
        yield return new WaitForSecondsRealtime(1f);
        countdownTmp.gameObject.SetActive(false);

        HideNeurons();

        countdownCo = null;
    }

    void HideNeurons()
    {
        for (int i = 0; i < NUM_NEURONS; i++)
        {
            HideNeuron(topNeurons[i], "top", i);
            HideNeuron(bottomNeurons[i], "bottom", i);
        }
    }

    void CheckCorrectness()
    {
        if (topChosen != -1 && bottomChosen != -1)
        {
            if (topChosen == bottomChosen)
            {
                if (learning == 2) {
                    isGameOver = true;
                    Debug.Log("Game over triggered");
                }
                DisplaySuccess();
                TriggerLearning();
            }
            else
            {
                DisplayError();
            }
            DeselectAllButtons();
        }
    }


    void DisplaySuccess()
    {
        if (resultCo != null) StopCoroutine(resultCo);
        resultCo = StartCoroutine(SuccessRoutine());
    }

    System.Collections.IEnumerator SuccessRoutine()
    {
        resultRunning = true;
        resultTmp.gameObject.SetActive(true);
        // resultImg.gameObject.SetActive(true);

        string text = "Perfect Connection";
        string condition = "Memory formed";
        text += "\n" + condition;
        resultTmp.text = text;
        yield return new WaitForSecondsRealtime(5f);

        resultTmp.gameObject.SetActive(false);
        // resultImg.gameObject.SetActive(false);

        if (!isGameOver)
        {
            ResetNeurons();
            InitiateCountdown();
        }

        resultCo = null;
        resultRunning = false;
    }

    void DisplayError()
    {
        if (resultCo != null) StopCoroutine(resultCo);
        resultCo = StartCoroutine(ErrorRoutine());
    }

    System.Collections.IEnumerator ErrorRoutine()
    {
        resultRunning = true;
        resultTmp.gameObject.SetActive(true);
        resultImg.gameObject.SetActive(true);

        string text = "Wrong connection";
        string condition = "Triggered Sleep";
        text += "\n" + condition;
        resultTmp.text = text;
        yield return new WaitForSecondsRealtime(3f);

        resultTmp.gameObject.SetActive(false);
        resultImg.gameObject.SetActive(false);

        HideNeurons();

        resultCo = null;
        resultRunning = false;
    }

    void TriggerLearning()
    {
        UpdateLearning();

        switch (learning)
        {
            case 0:
                FlickerUpdateText(nameTmp, "Katara");
                break;
            case 1:
                RevealAvatar();
                break;
            case 2:
                seqTicker.FlickerUpdateTicker("KARAENSENTHILKUMAR");
                EndGame();
                break;
        }

        learning++;
    }

    void UpdateLearning()
    {
        if (learningCo != null) StopCoroutine(learningCo);
        learningCo = StartCoroutine(LearningRoutine());
    }

    System.Collections.IEnumerator LearningRoutine()
    {
        int currentPercent = Mathf.RoundToInt(learning * (100f / 3));
        int targetPercent = Mathf.RoundToInt((learning + 1) * (100f / 3));
        float interval = 1f / Mathf.Max(1, 10);
        float elapsed = 0f;

        string suffix = "% learning";

        while (currentPercent < targetPercent)
        {
            currentPercent++;
            learningTmp.text = currentPercent + suffix;
            learningTmp.ForceMeshUpdate();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        learningCo = null;
    }

    void DeselectAllButtons()
    {
        topChosen = -1;
        bottomChosen = -1;
        EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            seqTicker.FlickerUpdateTicker("KARAENSENTHILKUMAR");
        }
    }

    public void FlickerUpdateText(TextMeshProUGUI tmp, string text)
    {
        FlickerUpdateText(tmp, text, 1f, 10);
    }

    public void FlickerUpdateText(TextMeshProUGUI tmp, string text, float duration, int cps)
    {
        if (flickerCo != null) StopCoroutine(flickerCo);
        flickerCo = StartCoroutine(FlickerRoutine(tmp, text, duration, cps));
    }

    System.Collections.IEnumerator FlickerRoutine(TextMeshProUGUI tmp, string finalText, float duration, int cps)
    {
        float interval = 1f / Mathf.Max(1, cps);
        float elapsed = 0f;

        int L = Mathf.Max(1, finalText.Length);
        var scratch = new System.Text.StringBuilder(L);

        while (elapsed < duration)
        {
            scratch.Clear();
            for (int i = 0; i < L; i++)
            {
                bool showReal = rng.NextDouble() < elapsed / duration * 0.6 + 0.3;
                char c = showReal ? finalText[i] : FLICKER_CHARS[rng.Next(FLICKER_CHARS.Length)];
                scratch.Append(c);
            }

            tmp.text = scratch.ToString();
            tmp.ForceMeshUpdate();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        tmp.text = finalText;
        tmp.ForceMeshUpdate();

        flickerCo = null;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void SpawnAvatar()
    {
        GameObject prefab = Resources.Load<GameObject>("Avatars/Jellyfish/jellyfish");

        if (prefab != null)
        {
            Vector3 pos = spawnPoint.position;

            currentAvatar = Instantiate(prefab, pos, prefab.transform.rotation, spawnPoint);
            SetLayerRecursively(currentAvatar, LayerMask.NameToLayer("MemoryGameUI"));

            // Collect all renderer materials
            var renderers = currentAvatar.GetComponentsInChildren<Renderer>();
            var mats = new System.Collections.Generic.List<Material>();
            foreach (var r in renderers)
                mats.AddRange(r.materials);

            avatarMaterials = mats.ToArray();
            origBaseColors = new Color[avatarMaterials.Length];
            origEmisColors = new Color[avatarMaterials.Length];

            // Save original colors, then set silhouette
            for (int i = 0; i < avatarMaterials.Length; i++)
            {
                origBaseColors[i] = avatarMaterials[i].color;

                if (avatarMaterials[i].HasProperty("_EmissionColor"))
                {
                    origEmisColors[i] = avatarMaterials[i].GetColor("_EmissionColor");
                    avatarMaterials[i].SetColor("_EmissionColor", Color.black);
                }

                avatarMaterials[i].color = Color.black;
            }
        }
    }

    public void RevealAvatar()
    {
        if (currentAvatar != null)
            StartCoroutine(FadeToColorRoutine());
    }

    private IEnumerator FadeToColorRoutine()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);

            for (int i = 0; i < avatarMaterials.Length; i++)
            {
                avatarMaterials[i].color = Color.Lerp(Color.black, origBaseColors[i], lerp);

                if (avatarMaterials[i].HasProperty("_EmissionColor"))
                {
                    Color startEmis = Color.black;
                    Color targetEmis = origEmisColors[i];
                    avatarMaterials[i].SetColor("_EmissionColor", Color.Lerp(startEmis, targetEmis, lerp));
                }
            }

            yield return null;
        }

        for (int i = 0; i < avatarMaterials.Length; i++)
        {
            avatarMaterials[i].color = origBaseColors[i];
            if (avatarMaterials[i].HasProperty("_EmissionColor"))
                avatarMaterials[i].SetColor("_EmissionColor", origEmisColors[i]);
        }
    }

    void EndGame()
    {
        if (endCo != null) StopCoroutine(endCo);
        endCo = StartCoroutine(EndGameRoutine());
    }

    System.Collections.IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(5f);
        TriggerFinalSequence();

        learningCo = null;
    }

    void TriggerFinalSequence()
    {
        if (dialogueController != null)
        {
            dialogueController.StartSequence("narrator-memory-final");
        }
    }
}
