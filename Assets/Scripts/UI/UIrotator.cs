using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class UIrotator : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cineCam;
    [SerializeField] private TextMeshProUGUI playerTag;
    private Vector3 offset = new Vector3(0, 180, 0);
    private bool isVisible = true;
    private int counter = 0;

    void Awake()
    {
        if (cineCam == null)
        {
            var allCams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            cineCam = System.Array.Find(allCams, c => c.name == "ThirdPersonCam");
        }
    }

    void OnBecameVisible()
    {
        isVisible = true;
        Debug.Log($"{playerTag.text} is visible");
    }

    void OnBecameInvisible()
    {
        isVisible = false;
    }

    void Update()
    {
        if (isVisible)
        {
            transform.LookAt(cineCam.transform);
            transform.Rotate(offset);   
        }
    }
}
