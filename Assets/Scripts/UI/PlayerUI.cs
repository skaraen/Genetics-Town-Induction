using System;
using DG.Tweening;
using Fusion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    private enum FacingTarget { Camera, Player, Hybrid }

    public static event Action<Transform> OnInteractPopupShown;
    public static event Action OnInteractPopupHidden;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI descTmp;
    [SerializeField] private Button interactBtn;
    [SerializeField] private Sprite replySprite;

    [Header("Trackers")]
    [SerializeField] private Camera worldCam;
    [SerializeField] private float triggerRadius = 5f;

    [Header("UI Fade")]
    [SerializeField] private float fadeSmoothTime = 0.2f;

    private CanvasGroup canvasGroup;
    private Action targetAction;
    private CinemachineInputAxisController inputAxisController;
    private FacingTarget facingTarget = FacingTarget.Camera;
    private bool requestReceived;
    private PlayerCustoms pendingTargetCustoms;
    private ClientRegistry registry;

    private void Start()
    {
        if (Object != null && Object.HasInputAuthority)
            return;

        if (!worldCam)
        {
            worldCam = CameraManager.Instance.RenderCamera;
            inputAxisController = CameraManager.Instance.InputAxisController;
        }

        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;

        UpdateTargetAction();
    }

    private bool IsOwnPlayer(Collider other)
    {
        if (!other.CompareTag("Player"))
            return false;

        NetworkObject networkObject = other.gameObject.GetComponentInParent<NetworkObject>();
        return networkObject != null && networkObject.HasInputAuthority;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOwnPlayer(other))
            ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsOwnPlayer(other))
            HideUI();
    }

    private void ShowUI()
    {
        if (inputAxisController) inputAxisController.enabled = false;
        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, fadeSmoothTime).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
        OnInteractPopupShown?.Invoke(canvas.transform);
    }

    private void HideUI()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeSmoothTime).OnComplete(() =>
        {
            if (inputAxisController) inputAxisController.enabled = true;
        });
        OnInteractPopupHidden?.Invoke();
    }

    private void OnEnable()
    {
        if (!canvas)
        {
            Debug.LogError("PlayerUI canvas not found");
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = worldCam;
        if (!canvas.TryGetComponent(out canvasGroup))
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        interactBtn.onClick.AddListener(OnInteractClicked);

        if (!registry) registry = FindFirstObjectByType<ClientRegistry>();
    }

    private void OnDisable()
    {
        if (interactBtn != null)
            interactBtn.onClick.RemoveListener(OnInteractClicked);
    }

    private void OnInteractClicked()
    {
        if (!requestReceived)
        {
            descTmp.text = "Waiting for response...";
            interactBtn.enabled = false;
            RPC_NotifyOwner();
            return;
        }

        requestReceived = false;
        Part2DataHolder.OwnerClientId = (ulong)Object.InputAuthority.PlayerId;
        Part2DataHolder.TargetCustoms = pendingTargetCustoms;
        RPC_AckOwner();
        SceneManager.LoadScene("Part2Scene", LoadSceneMode.Additive);
    }

    private void UpdateTargetAction()
    {
        targetAction = facingTarget switch
        {
            FacingTarget.Camera => FaceCamera,
            FacingTarget.Hybrid => FaceHybrid,
            _ => null
        };
    }

    private void FaceCamera()
    {
        if (!canvas || !worldCam) return;
        var toCam = canvas.transform.position - worldCam.transform.position;
        canvas.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
    }

    private void FaceHybrid()
    {
        if (!canvas || !worldCam) return;
        var camFwdFlat = Vector3.ProjectOnPlane(worldCam.transform.forward, Vector3.up);

        if (camFwdFlat.sqrMagnitude <= Vector3.kEpsilon) return;
        canvas.transform.rotation = Quaternion.LookRotation(camFwdFlat, Vector3.up);
    }

    private void LateUpdate()
    {
        targetAction?.Invoke();
    }

    public void RequestInteraction(PlayerCustoms targetCustoms)
    {
        requestReceived = true;
        pendingTargetCustoms = targetCustoms;
        descTmp.text = "says hello!\nPress reply to say hello back.";
        interactBtn.GetComponent<Image>().sprite = replySprite;
        interactBtn.enabled = true;
    }

    private static PlayerRef ResolveSource(NetworkRunner runner, RpcInfo info)
    {
        return info.Source == PlayerRef.None ? runner.LocalPlayer : info.Source;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_NotifyOwner(RpcInfo info = default)
    {
        PlayerRef sender = ResolveSource(Runner, info);

        PlayerCustoms senderCustoms = default;
        PlayerMetadataRegistry.Instance.TryGetCustoms(sender, out senderCustoms);

        RPC_ReceiveInteractionRequest(Object.InputAuthority, sender, senderCustoms);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveInteractionRequest([RpcTarget] PlayerRef target, PlayerRef sender, PlayerCustoms senderCustoms)
    {
        if (!registry) registry = FindFirstObjectByType<ClientRegistry>();

        GameObject targetPlayer = registry != null ? registry.FindPlayerByPlayerRef(sender) : null;
        targetPlayer?.GetComponent<PlayerUI>()?.RequestInteraction(senderCustoms);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_AckOwner(RpcInfo info = default)
    {
        PlayerRef sender = ResolveSource(Runner, info);

        PlayerCustoms senderCustoms = default;
        PlayerMetadataRegistry.Instance.TryGetCustoms(sender, out senderCustoms);

        RPC_ReceiveInteractionAck(Object.InputAuthority, sender, senderCustoms);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveInteractionAck([RpcTarget] PlayerRef target, PlayerRef sender, PlayerCustoms senderCustoms)
    {
        Part2DataHolder.TargetCustoms = senderCustoms;
        PlayerMetadataRegistry.Instance.RegisterInteractionServerRpc(sender);
        SceneManager.LoadScene("Part2Scene", LoadSceneMode.Additive);
    }
}
