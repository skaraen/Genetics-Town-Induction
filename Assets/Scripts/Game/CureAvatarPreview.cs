using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CureAvatarPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private RenderTexture renderTexture;

    [Header("Preview")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewStage;

    [Header("Avatar")]
    [SerializeField] private Vector3 avatarEuler = Vector3.zero;

    [Header("Manual Framing")]
    [SerializeField] private Vector3 avatarBaseLocalPosition = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector3 avatarLocalScale = new Vector3(0.45f, 0.45f, 0.45f);

    [Header("Avatar Profile Offset")]
    [SerializeField] private bool useAvatarProfileYOffset = true;
    [SerializeField] private float referenceCanvasYOffset = -0.25f;
    [SerializeField] private float profileYOffsetMultiplier = 0.3f;

    [Header("Materials / Effect")]
    [SerializeField] private Material holoMat;
    [SerializeField] private GameObject blindingEffectPrefab;

    [Header("Timing")]
    [SerializeField] private float holoDelay = 1.0f;
    [SerializeField] private float flashDelay = 0.35f;
    [SerializeField] private float afterRestoreDelay = 0.4f;
    [SerializeField] private float effectDestroyDelay = 0.5f;

    [Header("Dialogue UI")]
    [SerializeField] private DialogueController dialogueUI;
    [SerializeField] private string finalDialogueId = "narrator-aso-final";

    private GameObject currentAvatar;
    private GameObject currentEffect;

    private Renderer[] renderers;
    private readonly Dictionary<Renderer, Material[]> defaultMaterials = new();

    private Coroutine transitionRoutine;

    void OnEnable()
    {
        SetupPreview();
        ShowSelfAvatar();
    }

    void OnDisable()
    {
        Cleanup();
    }

    void SetupPreview()
    {
        if (previewRawImage != null)
            previewRawImage.texture = renderTexture;

        if (previewCamera != null)
        {
            previewCamera.targetTexture = renderTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }
    }

    void ShowSelfAvatar()
    {
        CleanupStageOnly();

        if (PlayerLogbook.Instance == null)
        {
            Debug.LogWarning("[CureAvatarPreview] PlayerLogbook instance not found.");
            return;
        }

        List<PlayerCustoms> customsList = PlayerLogbook.Instance.GetAllCustoms();

        if (customsList == null || customsList.Count == 0)
        {
            Debug.LogWarning("[CureAvatarPreview] No player customs found.");
            return;
        }

        if (previewStage == null)
        {
            Debug.LogWarning("[CureAvatarPreview] Preview stage not assigned.");
            return;
        }

        PlayerCustoms selfCustoms = customsList[0];

        currentAvatar = AvatarLoader.Load(
            selfCustoms,
            previewStage.position,
            Quaternion.identity,
            previewStage
        );

        if (currentAvatar == null)
        {
            Debug.LogWarning("[CureAvatarPreview] Failed to load self avatar.");
            return;
        }

        ApplyAvatarTransform();

        int previewLayer = LayerMask.NameToLayer("BubblePreview");
        if (previewLayer != -1)
            SetLayerRecursively(currentAvatar, previewLayer);

        CacheDefaultMaterials();

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(HoloToDefaultTransition());
    }

    void ApplyAvatarTransform()
    {
        if (currentAvatar == null)
            return;

        Vector3 finalPosition = avatarBaseLocalPosition;

        if (useAvatarProfileYOffset)
        {
            AvatarProfileData profile = currentAvatar.GetComponent<AvatarProfileData>();

            if (profile != null)
            {
                float adjustedOffset =
                    (profile.canvasYOffset - referenceCanvasYOffset) * profileYOffsetMultiplier;

                finalPosition.y += adjustedOffset;

                Debug.Log(
                    $"[CureAvatarPreview] Avatar: {currentAvatar.name}, " +
                    $"CanvasYOffset: {profile.canvasYOffset}, " +
                    $"AdjustedOffset: {adjustedOffset}, " +
                    $"FinalY: {finalPosition.y}"
                );
            }
            else
            {
                Debug.LogWarning("[CureAvatarPreview] AvatarProfileData not found on spawned avatar.");
            }
        }

        currentAvatar.transform.localPosition = finalPosition;
        currentAvatar.transform.localRotation = Quaternion.Euler(avatarEuler);
        currentAvatar.transform.localScale = avatarLocalScale;
    }

    void CacheDefaultMaterials()
    {
        defaultMaterials.Clear();

        if (currentAvatar == null)
            return;

        renderers = currentAvatar.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r != null)
                defaultMaterials[r] = r.materials;
        }
    }

    IEnumerator HoloToDefaultTransition()
    {
        ApplyHoloMaterial();

        yield return new WaitForSeconds(holoDelay);

        SpawnBlindingEffect();

        yield return new WaitForSeconds(flashDelay);

        RestoreDefaultMaterials();

        yield return new WaitForSeconds(afterRestoreDelay);

        StopBlindingEffect();

        yield return new WaitForSeconds(afterRestoreDelay);

        if (dialogueUI != null)
            dialogueUI.StartSequence(finalDialogueId);
    }

    void ApplyHoloMaterial()
    {
        if (holoMat == null || renderers == null)
            return;

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            Material[] mats = new Material[r.materials.Length];

            for (int i = 0; i < mats.Length; i++)
                mats[i] = holoMat;

            r.materials = mats;
        }
    }

    void RestoreDefaultMaterials()
    {
        if (renderers == null)
            return;

        foreach (Renderer r in renderers)
        {
            if (r != null && defaultMaterials.TryGetValue(r, out Material[] mats))
                r.materials = mats;
        }
    }

    void SpawnBlindingEffect()
    {
        if (blindingEffectPrefab == null || currentAvatar == null || previewStage == null)
            return;

        Bounds bounds = GetCombinedBounds(currentAvatar);
        Vector3 center = bounds.center;

        currentEffect = Instantiate(
            blindingEffectPrefab,
            center,
            Quaternion.identity,
            previewStage
        );

        currentEffect.transform.localScale = Vector3.one;

        int previewLayer = LayerMask.NameToLayer("BubblePreview");
        if (previewLayer != -1)
            SetLayerRecursively(currentEffect, previewLayer);
    }

    void StopBlindingEffect()
    {
        if (currentEffect == null)
            return;

        ParticleSystem[] particles = currentEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
            ps.Stop();

        Destroy(currentEffect, effectDestroyDelay);
        currentEffect = null;
    }

    Bounds GetCombinedBounds(GameObject obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>(true);

        if (rs.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = rs[0].bounds;

        for (int i = 1; i < rs.Length; i++)
            bounds.Encapsulate(rs[i].bounds);

        return bounds;
    }

    void CleanupStageOnly()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        if (previewStage != null)
        {
            for (int i = previewStage.childCount - 1; i >= 0; i--)
            {
                Destroy(previewStage.GetChild(i).gameObject);
            }
        }

        currentAvatar = null;
        currentEffect = null;
        renderers = null;
        defaultMaterials.Clear();
    }

    void Cleanup()
    {
        CleanupStageOnly();
    }

    static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}