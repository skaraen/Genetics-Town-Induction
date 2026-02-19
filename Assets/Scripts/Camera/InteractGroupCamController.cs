using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Owner-only controller that activates a dedicated group-framing camera when a PlayerUI interact
/// popup becomes visible, ensuring both the local player and the popup canvas are framed together.
/// When the popup hides, lowers camera priority and CinemachineBrain blends back to ThirdPersonCam.
/// </summary>
public class InteractGroupCamController : NetworkBehaviour
{
    [Tooltip("Priority raised to when the popup is visible. Must exceed ThirdPersonCam's owner priority (100).")]
    [SerializeField] private int activePriority = 200;

    [Tooltip("Priority when inactive — must be below all other active cameras.")]
    [SerializeField] private int inactivePriority = 0;

    [Tooltip("Seconds after popup hide before clearing the target group. " +
             "Must be >= CinemachineBrain blend duration (theres a custom blend for the group -> 3d) to avoid a visual pop mid-blend.")]
    [SerializeField] private float clearGroupDelay = 1f;

    private CinemachineCamera interactCam;
    private CinemachineTargetGroup targetGroup;
    private Coroutine clearGroupCoroutine;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        interactCam = CameraManager.Instance.InteractGroupCam;
        targetGroup = interactCam.Target.TrackingTarget.GetComponent<CinemachineTargetGroup>();

        PlayerUI.OnInteractPopupShown  += OnPopupShown;
        PlayerUI.OnInteractPopupHidden += OnPopupHidden;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        PlayerUI.OnInteractPopupShown  -= OnPopupShown;
        PlayerUI.OnInteractPopupHidden -= OnPopupHidden;

        SetCamState(false);
        ClearTargetGroup();
    }

    /// <summary>
    /// Called when a non-owner PlayerUI shows its interact popup to the local owner.
    /// Populates the target group with this player's transform and the popup canvas,
    /// then raises InteractGroupCam priority so CinemachineBrain blends to it.
    /// </summary>
    private void OnPopupShown(Transform popupCanvasTransform)
    {
        // Cancel any pending group-clear — rapid show-hide-show must not leave the group empty.
        if (clearGroupCoroutine != null)
        {
            StopCoroutine(clearGroupCoroutine);
            clearGroupCoroutine = null;
        }

        ClearTargetGroup();

        // Radius tells CinemachineGroupFraming how much space around each pivot to guarantee in-frame.
        targetGroup.AddMember(transform, weight: 1f, radius: 1f);
        targetGroup.AddMember(popupCanvasTransform, weight: 1f, radius: 0.5f);

        // activate the camera
        interactCam.Priority.Value   = activePriority;
        interactCam.Priority.Enabled = true;
        SetCamState(true);
    }

    /// <summary>
    /// Called when the interact popup begins hiding.
    /// Lowers InteractGroupCam priority immediately so the blend back to ThirdPersonCam starts,
    /// then clears the group after the blend completes to avoid popping the target mid-blend.
    /// </summary>
    private void OnPopupHidden()
    {
        SetCamState(false);
        if (clearGroupCoroutine != null) StopCoroutine(clearGroupCoroutine);
        clearGroupCoroutine = StartCoroutine(ClearGroupAfterDelay());
    }

    private void SetCamState(bool enabled = true)
    {
        interactCam.Priority.Value = enabled ? activePriority : inactivePriority;
        interactCam.Priority.Enabled = true;
    }

    /// <summary>Removes all members from the target group, freeing their Transform references.</summary>
    private void ClearTargetGroup()
    {
        for (int i = targetGroup.Targets.Count - 1; i >= 0; i--)
            targetGroup.RemoveMember(targetGroup.Targets[i].Object);
    }

    private IEnumerator ClearGroupAfterDelay()
    {
        yield return new WaitForSeconds(clearGroupDelay);
        ClearTargetGroup();
        clearGroupCoroutine = null;
    }
}
