using System;
using System.ComponentModel;
using System.Security.Cryptography;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    private enum FacingTarget { Camera, Player, Hybrid }

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
    bool pendingHide;
    float targetAlpha, currentAlpha, alphaVelocity;

    SphereCollider trigger;
    Action targetAction;
    CinemachineInputAxisController inputAxisController;
    FacingTarget facingTarget = FacingTarget.Camera;

    bool requestReceived = false;
    int pendingTargetAvatarId; // possible avatar id of the other play
    ClientRegistry registry;

    void Start()
    {
        if (gameObject.GetComponent<NetworkObject>().IsOwner) return;

        if (!worldCam)
        {
            var brain = FindFirstObjectByType<CinemachineBrain>();
            var cineCam = FindFirstObjectByType<CinemachineCamera>();

            if (brain) worldCam = brain.GetComponent<Camera>();
            if (cineCam) inputAxisController = cineCam.GetComponent<CinemachineInputAxisController>();

            if (!worldCam) worldCam = Camera.main;
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

    void ShowUI()
    {
        canvas.gameObject.SetActive(true);
        if (inputAxisController) inputAxisController.enabled = false;
        targetAlpha = 1f;
    }

    void HideUI()
    {
        targetAlpha = 0f;
        pendingHide = true;
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
        canvas.gameObject.SetActive(false);
        if (!canvas.TryGetComponent<CanvasGroup>(out canvasGroup))
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup)
        {
            currentAlpha = targetAlpha = 0f;
            alphaVelocity = 0f;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
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
                Part2DataHolder.TargetAvatarId = pendingTargetAvatarId;
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
        if (canvas.gameObject.activeSelf) targetAction?.Invoke();
    }

    void Update()
    {
        if (canvasGroup)
        {
            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, fadeSmoothTime);
            canvasGroup.alpha = currentAlpha;

            bool interact = currentAlpha > 0.5f;
            canvasGroup.interactable = interact;
            canvasGroup.blocksRaycasts = interact;
        }

        if (pendingHide)
        {
            bool alphaDone = !canvasGroup || currentAlpha < 0.01f | Mathf.Approximately(currentAlpha, 0f);

            if (alphaDone)
            {
                canvas.gameObject.SetActive(false);
                if (inputAxisController) inputAxisController.enabled = true;
                pendingHide = false;
            }
        }
    }

    /// <summary>
    /// Marks this UI as having a pending interaction request and stores the requester's avatar ID
    /// so it can be forwarded to Part2DataHolder when accepted.
    /// </summary>
    /// <param name="targetAvatarId">The avatar profile ID of the player who sent the request</param>
    public void RequestInteraction(int targetAvatarId)
    {
        requestReceived = true;
        pendingTargetAvatarId = targetAvatarId;
        descTmp.text = "wants to interact with you";
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyOwnerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        int senderAvatarType = 0;
        if (PlayerMetadataRegistry.Instance.TryGetCustoms(senderId, out PlayerCustoms senderData))
            senderAvatarType = senderData.avatarType;

        NotifyOwnerClientRpc(senderId, senderAvatarType, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void NotifyOwnerClientRpc(ulong senderId, int senderAvatarType, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Owner {OwnerClientId} received notification from Client {senderId}");
        GameObject targetPlayer = registry.FindPlayerByClientId(senderId);

        targetPlayer.GetComponent<PlayerUI>().RequestInteraction(senderAvatarType);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AckOwnerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        int senderAvatarType = 0;
        if (PlayerMetadataRegistry.Instance.TryGetCustoms(senderId, out PlayerCustoms senderData))
            senderAvatarType = senderData.avatarType;

        AckOwnerClientRpc(senderId, senderAvatarType, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void AckOwnerClientRpc(ulong senderId, int senderAvatarType, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Owner {OwnerClientId} received acknowledgement from Client {senderId}");
        Part2DataHolder.TargetAvatarId = senderAvatarType;
        SceneManager.LoadScene("Part2Scene", LoadSceneMode.Additive);
    }
}
