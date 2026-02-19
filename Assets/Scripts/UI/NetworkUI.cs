using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Initilizes the buttons in the title screen menu (for testing purposes)
/// which starts the game as a host, server, or client.
/// Supports LAN multiplayer: host listens on all interfaces (0.0.0.0),
/// client connects to the IP entered in the input field.
/// </summary>
public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ServerButton;
    [SerializeField] private Button ClientButton;

    [Header("LAN")]
    [Tooltip("Input field for the host's LAN IP address (used when joining as a client).")]
    [SerializeField] private TMP_InputField ipInputField; // default placeholder: 127.0.0.1

    [Header("Scenes")]
    [SerializeField] private string avatarSceneName = "IntroScene";

    [Header("Network Specs")]
    [SerializeField]
    public string hostIp = "10.150.3.226";
    public ushort port = 7777;

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
            // Listen on all network interfaces so other LAN devices can reach this host.
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData("0.0.0.0", 7777);
            if (NetworkManager.Singleton.StartHost())
            {
                // Server/Host drives scene change; clients follow automatically.
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
            // Use the IP from the input field, falling back to localhost for same-machine testing.
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            string ip = (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
                ? ipInputField.text.Trim()
                : "127.0.0.1";
            transport.SetConnectionData(ip, 7777);
            NetworkManager.Singleton.StartClient();
        });
    }
}
