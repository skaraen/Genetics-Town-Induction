using System;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    private enum FacingTarget { Camera, Player, Hybrid }

    /// <summary>
    /// Fired on the local machine when this player's interact popup becomes visible to the local owner.
    /// Passes the canvas Transform so the camera controller can frame it alongside the local player.
    /// Static so InteractGroupCamController on the local player can subscribe without a reference to this instance.
    /// </summary>
    public static event System.Action<Transform> OnInteractPopupShown;

    /// <summary>
    /// Fired on the local machine when this player's interact popup begins hiding.
    /// Fires at the start of HideUI() (not after fade) so the camera blend back begins immediately.
    /// </summary>
    public static event System.Action OnInteractPopupHidden;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI descTmp;
    [SerializeField] private Button interactBtn;

    [Header("Trackers")]
    [SerializeField] private Camera worldCam;
    [SerializeField] private float triggerRadius = 5f;

    [Header("UI Fade")]
    [SerializeField] float fadeSmoothTime = 0.2f;
    CanvasGroup canvasGroup;

    SphereCollider trigger;
    Action targetAction;
    CinemachineInputAxisController inputAxisController;
    FacingTarget facingTarget = FacingTarget.Camera;

    bool requestReceived = false;
    PlayerCustoms pendingTargetCustoms; // customs of the other player
    ClientRegistry registry;

    void Start()
    {
        if (gameObject.GetComponent<NetworkObject>().IsOwner) return;

        if (!worldCam)
        {
            worldCam = CameraManager.Instance.RenderCamera;
            inputAxisController = CameraManager.Instance.InputAxisController;
        }

        trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;

        UpdateTargetAction();
    }

    bool IsOwnPlayer(Collider other)
    {
        if (!other.CompareTag("Player") || !other.gameObject.GetComponentInParent<NetworkObject>().IsOwner)
            return false;

        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsOwnPlayer(other)) return;

        Debug.Log("Trigger enter");
        ShowUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsOwnPlayer(other)) return;

        Debug.Log("Trigger exit");
        HideUI();
    }

    /// <summary>
    /// Activates the canvas and fades it in via DOTween. Stops any in-progress fade first.
    /// </summary>
    void ShowUI()
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

    /// <summary>
    /// Fades the canvas out via DOTween, then deactivates it. Stops any in-progress fade first.
    /// Fires OnInteractPopupHidden immediately so camera blends back without waiting for the fade.
    /// </summary>
    void HideUI()
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

    void OnEnable()
    {
        if (!canvas)
        {
            Debug.LogError("PlayerUI canvas not found");
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = worldCam;
        if (!canvas.TryGetComponent<CanvasGroup>(out canvasGroup))
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        interactBtn.onClick.AddListener(() =>
        {
            // if you interact on another player
            if (!requestReceived)
            {
                descTmp.text = "Waiting for response...";
                interactBtn.enabled = false;
                NotifyOwnerServerRpc(); // notify the other player
            }
            // If another player interacts on you
            else
            {
                requestReceived = false;
                Part2DataHolder.TargetCustoms = pendingTargetCustoms;
                AckOwnerServerRpc();
                // PROBLEM: the scene is not additively loaded correctly, 
                // you can see grass from the game in the background
                SceneManager.LoadScene("Part2Scene", LoadSceneMode.Additive);
            }
        });

        if (!registry) registry = FindFirstObjectByType<ClientRegistry>();
    }

    void OnDisable()
    {

    }

    void UpdateTargetAction()
    {
        targetAction = facingTarget switch
        {
            FacingTarget.Camera => FaceCamera,
            FacingTarget.Hybrid => FaceHybrid,
            _ => null
        };
    }

    void FaceCamera()
    {
        if (!canvas || !worldCam) return;
        var toCam = canvas.transform.position - worldCam.transform.position;
        canvas.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
    }

    void FaceHybrid()
    {
        if (!canvas || !worldCam) return;
        var camFwdFlat = Vector3.ProjectOnPlane(worldCam.transform.forward, Vector3.up);

        if (camFwdFlat.sqrMagnitude <= Vector3.kEpsilon) return;
        canvas.transform.rotation = Quaternion.LookRotation(camFwdFlat, Vector3.up);
    }

    void LateUpdate()
    {
        targetAction?.Invoke();
    }


    /// <summary>
    /// Marks this UI as having a pending interaction request and stores the requester's full
    /// customization data so it can be forwarded to Part2DataHolder when accepted.
    /// </summary>
    /// <param name="targetCustoms">The full visual customization of the player who sent the request</param>
    public void RequestInteraction(PlayerCustoms targetCustoms)
    {
        requestReceived = true;
        pendingTargetCustoms = targetCustoms;
        descTmp.text = "wants to interact with you";
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyOwnerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        PlayerCustoms senderCustoms = default;
        PlayerMetadataRegistry.Instance.TryGetCustoms(senderId, out senderCustoms);

        NotifyOwnerClientRpc(senderId, senderCustoms, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void NotifyOwnerClientRpc(ulong senderId, PlayerCustoms senderCustoms, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Owner {OwnerClientId} received notification from Client {senderId}");
        GameObject targetPlayer = registry.FindPlayerByClientId(senderId);

        targetPlayer.GetComponent<PlayerUI>().RequestInteraction(senderCustoms);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AckOwnerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        PlayerCustoms senderCustoms = default;
        PlayerMetadataRegistry.Instance.TryGetCustoms(senderId, out senderCustoms);

        AckOwnerClientRpc(senderId, senderCustoms, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void AckOwnerClientRpc(ulong senderId, PlayerCustoms senderCustoms, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Owner {OwnerClientId} received acknowledgement from Client {senderId}");
        Part2DataHolder.TargetCustoms = senderCustoms;
        SceneManager.LoadScene("Part2Scene", LoadSceneMode.Additive);
    }
}
