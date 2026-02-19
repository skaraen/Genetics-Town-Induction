using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class Player : NetworkBehaviour
{
    [SerializeField] private float moveSpeed   = 7.0f;
    [SerializeField] private float rotateSpeed = 7.0f;
    [SerializeField] private float playerHeight = 0.7f;
    [SerializeField] private float playerRadius = 0.7f;
    [SerializeField] private float heightOffset = 1.2f;
    [SerializeField] private float ySmoothTime = 0.05f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayLength = 200f;
    [SerializeField] private Transform trackingTarget;
    [SerializeField] private int ownerCamPriority = 100;

    private CinemachineCamera cineCam;
    private bool isMoving;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        BindCamera();
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    /// <summary>Binds the ThirdPersonCam to this player's tracking target via CameraManager.</summary>
    private void BindCamera()
    {
        cineCam = CameraManager.Instance.ThirdPersonCam;
        var target = trackingTarget != null ? trackingTarget : transform;

        cineCam.Target.TrackingTarget = target;
        cineCam.Target.LookAtTarget   = target;
        cineCam.Priority              = ownerCamPriority;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && cineCam != null)
        {
            cineCam.Target.TrackingTarget = null;
            cineCam.Target.LookAtTarget   = null;
        }
    }

    private float _yVel; // for SmoothDamp

    private void HandleMovement()
    {
        // Camera-relative planar move
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 forward = cineCam.transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cineCam.transform.right;   right.y   = 0f; right.Normalize();

        Vector3 moveVector   = forward * inputVector.y + right * inputVector.x;
        float   moveDistance = moveSpeed * Time.deltaTime;

        // Move horizontally without collision checks (no obstruction)
        Vector3 targetXZ = transform.position + moveVector * moveDistance;

        // Sample ground height under the target XZ
        float groundY = GetGroundHeight(targetXZ, out bool hasGround);

        // If we found ground, keep a smooth clearance; otherwise preserve current Y
        float targetY = hasGround ? groundY + heightOffset : transform.position.y;
        float newY    = Mathf.SmoothDamp(transform.position.y, targetY, ref _yVel, ySmoothTime);

        // Commit position
        transform.position = new Vector3(targetXZ.x, newY, targetXZ.z);

        // Rotate only when actually moving
        if (moveVector != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveVector, Time.deltaTime * rotateSpeed);
        }

        isMoving = moveVector != Vector3.zero;
    }

    // Prefer Terrain.SampleHeight if available; fall back to a downward raycast.
    private float GetGroundHeight(Vector3 atXZ, out bool hasGround)
    {
        // 1) Terrain path (handles multi-terrain if activeTerrain is the one under us)
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Vector3 tPos = t.transform.position;
            Vector3 local = atXZ - new Vector3(tPos.x, 0f, tPos.z);

            // Inside terrain bounds?
            Vector3 size = t.terrainData.size;
            if (local.x >= 0f && local.z >= 0f && local.x <= size.x && local.z <= size.z)
            {
                float h = t.SampleHeight(atXZ) + tPos.y;
                hasGround = true;
                return h;
            }
        }

        // 2) Raycast path (works for meshes/other colliders)
        Vector3 rayOrigin = atXZ + Vector3.up * groundRayLength * 0.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            hasGround = true;
            return hit.point.y;
        }

        hasGround = false;
        return atXZ.y; // fallback
    }

    private void HandleInteractions()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] proximalNpcs = Physics.OverlapSphere(transform.position, 2f);
            foreach (var c in proximalNpcs)
                Debug.Log(c.name);
        }
    }
}
