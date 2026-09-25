using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Starts Photon Fusion internet sessions through a short lobby-code flow.
/// Hosts generate a code in the AA00 format, and clients join that exact session name.
/// </summary>
public class NetworkUI : MonoBehaviour
{
    private const string CodePattern = "^[A-Z]{2}[0-9]{2}$";
    private const string GeneticsTownAppId = "b37d1e27-1e52-4ed4-88e2-4ee026899d2f";

    [SerializeField] private Button HostButton;
    [SerializeField] private Button ServerButton;
    [SerializeField] private Button ClientButton;

    [Header("Lobby Code")]
    [Tooltip("Input field for the AA00 lobby code. Existing LAN IP field references can be reused here.")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text statusText;

    [Header("Photon Fusion")]
    [SerializeField] private string customLobbyName = "GeneticsTown";
    [SerializeField] private int maxPlayers = 4;

    [Header("Scenes")]
    [SerializeField] private string avatarSceneName = "IntroScene";

    private static readonly Regex LobbyCodeRegex = new Regex(CodePattern, RegexOptions.Compiled);
    private NetworkRunner runner;
    private bool isStarting;

    private void Start()
    {
        ConfigureLobbyControls();

        HostButton.onClick.AddListener(CreateLobby);
        ClientButton.onClick.AddListener(JoinLobby);

        if (ServerButton != null)
            ServerButton.onClick.AddListener(LeaveLobby);

        SetStatus("Create a lobby or enter a code to join.");
    }

    private void CreateLobby()
    {
        string code = GenerateLobbyCode();

        if (ipInputField != null)
            ipInputField.text = code;

        SetLobbyCode(code);
        _ = StartFusionSession(GameMode.Host, code, allowClientSessionCreation: true);
    }

    private void JoinLobby()
    {
        string code = NormalizeLobbyCode(ipInputField != null ? ipInputField.text : string.Empty);

        if (!IsValidLobbyCode(code))
        {
            SetStatus("Enter a lobby code in AA00 format.");
            return;
        }

        if (ipInputField != null)
            ipInputField.text = code;

        SetLobbyCode(code);
        _ = StartFusionSession(GameMode.Client, code, allowClientSessionCreation: false);
    }

    private async Task StartFusionSession(GameMode gameMode, string code, bool allowClientSessionCreation)
    {
        if (isStarting)
            return;

        isStarting = true;
        SetButtonsInteractable(false);
        SetStatus(gameMode == GameMode.Host ? $"Creating lobby {code}..." : $"Joining lobby {code}...");

        runner = EnsureRunner();
        runner.ProvideInput = true;

        NetworkSceneManagerDefault sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = code,
            PlayerCount = maxPlayers,
            IsOpen = true,
            IsVisible = false,
            CustomLobbyName = customLobbyName,
            EnableClientSessionCreation = gameMode == GameMode.Client ? allowClientSessionCreation : null,
            SceneManager = sceneManager
        });

        isStarting = false;

        if (result.Ok)
        {
            SetStatus(gameMode == GameMode.Host ? $"Lobby {code} created." : $"Joined lobby {code}.");
            if (runner.IsSceneAuthority)
                runner.LoadScene(avatarSceneName, LoadSceneMode.Single);
            return;
        }

        SetStatus($"Could not {(gameMode == GameMode.Host ? "create" : "join")} lobby {code}: {result.ShutdownReason}");
        await ShutdownRunner();
        SetButtonsInteractable(true);
    }

    private NetworkRunner EnsureRunner()
    {
        NetworkRunner existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null)
            return existingRunner;

        GameObject runnerObject = new GameObject("Photon Fusion Runner");
        DontDestroyOnLoad(runnerObject);
        return runnerObject.AddComponent<NetworkRunner>();
    }

    private async void LeaveLobby()
    {
        await ShutdownRunner();
        SetButtonsInteractable(true);
        SetStatus("Left lobby.");
    }

    private void ConfigureLobbyControls()
    {
        SetButtonLabel(HostButton, "Create Lobby");
        SetButtonLabel(ClientButton, "Join Lobby");
        SetButtonLabel(ServerButton, "Leave");

        if (ipInputField == null)
            return;

        ipInputField.characterLimit = 4;
        ipInputField.onValueChanged.AddListener(OnLobbyCodeChanged);
    }

    private void OnLobbyCodeChanged(string rawCode)
    {
        string normalizedCode = NormalizeLobbyCode(rawCode);

        if (normalizedCode.Length > 4)
            normalizedCode = normalizedCode.Substring(0, 4);

        if (ipInputField.text == normalizedCode)
            return;

        ipInputField.SetTextWithoutNotify(normalizedCode);
    }

    private async Task ShutdownRunner()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null)
            return;

        await runner.Shutdown();
        Destroy(runner.gameObject);
        runner = null;
    }

    private static string GenerateLobbyCode()
    {
        char firstLetter = (char)UnityEngine.Random.Range('A', 'Z' + 1);
        char secondLetter = (char)UnityEngine.Random.Range('A', 'Z' + 1);
        int number = UnityEngine.Random.Range(0, 100);

        return $"{firstLetter}{secondLetter}{number:00}";
    }

    private static string NormalizeLobbyCode(string rawCode)
    {
        return string.IsNullOrWhiteSpace(rawCode)
            ? string.Empty
            : rawCode.Trim().ToUpperInvariant();
    }

    private static bool IsValidLobbyCode(string code)
    {
        return LobbyCodeRegex.IsMatch(code);
    }

    private void SetLobbyCode(string code)
    {
        if (lobbyCodeText != null)
            lobbyCodeText.text = code;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"[NetworkUI] {message} AppId={GeneticsTownAppId}");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (HostButton != null)
            HostButton.interactable = interactable;

        if (ClientButton != null)
            ClientButton.interactable = interactable;

        if (ServerButton != null)
            ServerButton.interactable = true;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = label;
    }
}
