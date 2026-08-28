using UnityEngine;
using Fusion;
using Unity.Cinemachine;

public class Player : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(ApplyNetworkTransform))]
    private Vector3 NetworkedPosition { get; set; }

    [Networked, OnChangedRender(nameof(ApplyNetworkTransform))]
    private Quaternion NetworkedRotation { get; set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7.0f;
    [SerializeField] private float rotateSpeed = 7.0f;
    [SerializeField] private float playerHeight = 0.7f;
    [SerializeField] private float playerRadius = 0.7f;
    [SerializeField] private float groundOffset = 0.5f;
    [SerializeField] private float ySmoothTime = 0.05f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayLength = 200f;

    [Header("Camera")]
    [SerializeField] private Transform trackingTarget;
    [SerializeField] private int ownerCamPriority = 100;

    [Header("Avatar")]
    [SerializeField] private int avatarTypeId;

    [Header("Environment Audio")]
    [SerializeField] private float waterFadeDistance = 8f;
    [SerializeField] private float maxWaterVolume = 1f;
    [SerializeField] private float waterVolumeSmoothSpeed = 5f;

    private CinemachineCamera cineCam;

    private bool isMoving;
    private bool wasMoving;

    private float currentWaterVolume;
    private float _yVel;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
        }

        if (!Object.HasInputAuthority) return;

        BindCamera();

        EnvironmentSFXManager.Instance?.SetWalkingClipByAvatarType(avatarTypeId);
    }

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority) return;

        HandleMovement();
        HandleMovementAudio();
        HandleTerrainBoundaryWaterAudio();
        HandleInteractions();
    }

    /// <summary>Binds the ThirdPersonCam to this player's tracking target via CameraManager.</summary>
    private void BindCamera()
    {
        cineCam = CameraManager.Instance.ThirdPersonCam;
        var target = trackingTarget != null ? trackingTarget : transform;

        cineCam.Target.TrackingTarget = target;
        cineCam.Target.LookAtTarget = target;
        cineCam.Priority = ownerCamPriority;
    }

    private void OnDisable()
    {
        if (Object != null && Object.HasInputAuthority && cineCam != null)
        {
            cineCam.Target.TrackingTarget = null;
            cineCam.Target.LookAtTarget = null;
        }

        if (Object != null && Object.HasInputAuthority)
        {
            EnvironmentSFXManager.Instance?.StopWalking();
            EnvironmentSFXManager.Instance?.StopWater();
        }
    }

    private void HandleMovement()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 forward = cineCam.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cineCam.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveVector = forward * inputVector.y + right * inputVector.x;
        float moveDistance = moveSpeed * Time.deltaTime;

        Vector3 targetXZ = transform.position + moveVector * moveDistance;

        float groundY = GetGroundHeight(targetXZ, out bool hasGround);

        float targetY = hasGround ? groundY + groundOffset : transform.position.y;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _yVel, ySmoothTime);

        Vector3 nextPosition = new Vector3(targetXZ.x, newY, targetXZ.z);
        Quaternion nextRotation = transform.rotation;

        if (moveVector != Vector3.zero)
        {
            nextRotation = Quaternion.LookRotation(Vector3.Slerp(
                transform.forward,
                moveVector,
                Time.deltaTime * rotateSpeed
            ));
        }

        transform.SetPositionAndRotation(nextPosition, nextRotation);
        PublishTransform(nextPosition, nextRotation);

        isMoving = moveVector.sqrMagnitude > 0.001f;
    }

    private void PublishTransform(Vector3 position, Quaternion rotation)
    {
        if (Object.HasStateAuthority)
        {
            SetNetworkTransform(position, rotation);
            return;
        }

        RPC_Move(position, rotation);
    }

    private void SetNetworkTransform(Vector3 position, Quaternion rotation)
    {
        NetworkedPosition = position;
        NetworkedRotation = rotation;
    }

    private void ApplyNetworkTransform()
    {
        if (Object != null && Object.HasInputAuthority)
            return;

        transform.SetPositionAndRotation(NetworkedPosition, NetworkedRotation);
    }

    private void HandleMovementAudio()
    {
        EnvironmentSFXManager.Instance?.SetWalkingClipByAvatarType(avatarTypeId);

        if (isMoving && !wasMoving)
        {
            EnvironmentSFXManager.Instance?.StartWalking();
        }
        else if (!isMoving && wasMoving)
        {
            EnvironmentSFXManager.Instance?.StopWalking();
        }

        wasMoving = isMoving;
    }

    private void HandleTerrainBoundaryWaterAudio()
    {
        float distanceToBoundary = GetDistanceToTerrainBoundary(transform.position, out bool insideTerrain);

        if (!insideTerrain)
        {
            currentWaterVolume = Mathf.Lerp(
                currentWaterVolume,
                maxWaterVolume,
                Time.deltaTime * waterVolumeSmoothSpeed
            );

            EnvironmentSFXManager.Instance?.StartWater();
            EnvironmentSFXManager.Instance?.SetWaterVolume(currentWaterVolume);
            return;
        }

        float targetVolume = 0f;

        if (distanceToBoundary <= waterFadeDistance)
        {
            float closeness = 1f - Mathf.Clamp01(distanceToBoundary / waterFadeDistance);
            targetVolume = closeness * maxWaterVolume;
        }

        currentWaterVolume = Mathf.Lerp(
            currentWaterVolume,
            targetVolume,
            Time.deltaTime * waterVolumeSmoothSpeed
        );

        if (currentWaterVolume > 0.01f)
        {
            EnvironmentSFXManager.Instance?.StartWater();
            EnvironmentSFXManager.Instance?.SetWaterVolume(currentWaterVolume);
        }
        else
        {
            currentWaterVolume = 0f;
            EnvironmentSFXManager.Instance?.SetWaterVolume(0f);
            EnvironmentSFXManager.Instance?.StopWater();
        }
    }

    private float GetDistanceToTerrainBoundary(Vector3 worldPosition, out bool insideTerrain)
    {
        insideTerrain = false;

        if (Terrain.activeTerrain == null)
            return Mathf.Infinity;

        Terrain terrain = Terrain.activeTerrain;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float minX = terrainPos.x;
        float maxX = terrainPos.x + terrainSize.x;
        float minZ = terrainPos.z;
        float maxZ = terrainPos.z + terrainSize.z;

        float x = worldPosition.x;
        float z = worldPosition.z;

        insideTerrain = x >= minX && x <= maxX && z >= minZ && z <= maxZ;

        if (!insideTerrain)
            return 0f;

        float distanceToLeft = x - minX;
        float distanceToRight = maxX - x;
        float distanceToBottom = z - minZ;
        float distanceToTop = maxZ - z;

        return Mathf.Min(
            distanceToLeft,
            distanceToRight,
            distanceToBottom,
            distanceToTop
        );
    }

    private float GetGroundHeight(Vector3 atXZ, out bool hasGround)
    {
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Vector3 tPos = t.transform.position;
            Vector3 local = atXZ - new Vector3(tPos.x, 0f, tPos.z);

            Vector3 size = t.terrainData.size;

            if (local.x >= 0f && local.z >= 0f && local.x <= size.x && local.z <= size.z)
            {
                float h = t.SampleHeight(atXZ) + tPos.y;
                hasGround = true;
                return h;
            }
        }

        Vector3 rayOrigin = atXZ + Vector3.up * groundRayLength * 0.5f;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundRayLength,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            hasGround = true;
            return hit.point.y;
        }

        hasGround = false;
        return atXZ.y;
    }

    private void HandleInteractions()
    {
        if (Object == null || !Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] proximalNpcs = Physics.OverlapSphere(transform.position, 2f);

            foreach (var c in proximalNpcs)
                Debug.Log(c.name);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_Move(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        SetNetworkTransform(position, rotation);
    }
}
