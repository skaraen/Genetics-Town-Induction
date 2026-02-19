using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Scene-level singleton providing centralized access to all Cinemachine cameras and the render camera.
/// Attach to the camera parent GameObject and wire references in the inspector.
/// Eliminates fragile string-based camera lookups throughout the codebase.
/// </summary>
public class CameraManager : MonoBehaviour
{
    private static CameraManager _instance;

    /// <summary>Lazy-initialized singleton. Falls back to a scene search if Awake hasn't run yet.</summary>
    public static CameraManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<CameraManager>();
            return _instance;
        }
    }

    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera thirdPersonCam;
    [SerializeField] private CinemachineCamera interactGroupCam;

    /// <summary>The single CinemachineBrain driving camera blends in this scene.</summary>
    public CinemachineBrain Brain => cinemachineBrain;

    /// <summary>The Unity Camera used for rendering, derived from the CinemachineBrain.</summary>
    public Camera RenderCamera { get; private set; }

    /// <summary>The main third-person follow/look camera for the local player.</summary>
    public CinemachineCamera ThirdPersonCam => thirdPersonCam;

    /// <summary>The group-framing camera activated during interact popups.</summary>
    public CinemachineCamera InteractGroupCam => interactGroupCam;

    /// <summary>Input axis controller on the ThirdPersonCam, cached for enable/disable during UI interactions.</summary>
    public CinemachineInputAxisController InputAxisController { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        RenderCamera = cinemachineBrain.GetComponent<Camera>();
        InputAxisController = thirdPersonCam.GetComponent<CinemachineInputAxisController>();
    }
}
