using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class OwnerCineCamBinderV3 : NetworkBehaviour
{
    [SerializeField] private Transform trackingTarget;

    private CinemachineCamera cineCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        BindCamera();
        StartCoroutine(BindNextFrame());
    }

    private System.Collections.IEnumerator BindNextFrame()
    {
        yield return null;
        BindCamera();
    }

    private void BindCamera()
    {
            cineCam = FindFirstObjectByType<CinemachineCamera>();

        if (cineCam == null)
        {
            Debug.LogWarning("[CamBinder] No CinemachineCamera found in scene.");
            return;
        }

        var target = trackingTarget != null ? trackingTarget : transform;

        cineCam.Target.TrackingTarget = target;
        cineCam.Target.LookAtTarget   = target;
        cineCam.Priority = 100;            
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && cineCam != null)
        {
            cineCam.Target.TrackingTarget = null;
            cineCam.Target.LookAtTarget   = null;
        }
    }
}
