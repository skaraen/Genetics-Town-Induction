using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Initilizes the buttons in the title screen menu (for testing purposes)
/// which starts the game as a host, server, or client
/// </summary>
public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ServerButton;
    [SerializeField] private Button ClientButton;

    [Header("Scenes")]
    [SerializeField] private string avatarSceneName = "IntroScene"; // set in Inspector

    void Awake()
    {
        // 1) Disable auto player spawn on connect
        NetworkManager.Singleton.ConnectionApprovalCallback += Approval;

        // (Optional) If you want to spawn players right after GameScene loads,
        // you can hook OnLoadEventCompleted in some server-side spawner script.
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= Approval;
    }

    private void Approval(NetworkManager.ConnectionApprovalRequest req,
                          NetworkManager.ConnectionApprovalResponse res)
    {
        res.Approved = true;
        res.CreatePlayerObject = false; // <- prevent default player prefab from spawning
        // res.PlayerPrefabHash = default; // use default
        // res.Position/Rotation can be left unset
        res.Pending = false;
    }

    void Start()
    {
        HostButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                // 2) Server/Host drives scene change; clients follow automatically
                NetworkManager.Singleton.SceneManager.LoadScene(avatarSceneName, LoadSceneMode.Single);
            }
        });

        ServerButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(avatarSceneName, LoadSceneMode.Single);
            }
        });

        ClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            // Do NOT load scenes here; server will.
        });
    }
}
