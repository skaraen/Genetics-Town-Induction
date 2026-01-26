using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerVisual : NetworkBehaviour
{
    public NetworkVariable<PlayerCustoms> AvatarCustoms = new(
        new PlayerCustoms(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] TextMeshProUGUI playerTag;
    [SerializeField] private Material holoMat;
    Dictionary<string, Material> defaultMaterial = new Dictionary<string, Material>();
    Renderer[] renderers;
    public NetworkVariable<bool> IsInfected = new NetworkVariable<bool>(false, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    GameObject currentAvatar;
    GameObject btnObj;

    public override void OnNetworkSpawn()
    {
        AvatarCustoms.OnValueChanged += OnAvatarTypeChanged;
        IsInfected.OnValueChanged += OnInfectionToggle;

        btnObj = GameObject.Find("ASOBtn");
        if (btnObj != null)
        {
            Button button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene("ASOGame");
                });
                btnObj.SetActive(false);
            }
            else
            {
                Debug.LogError("No Button component found on object: MyButton");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        AvatarCustoms.OnValueChanged -= OnAvatarTypeChanged;
        IsInfected.OnValueChanged -= OnInfectionToggle;
        if (currentAvatar) Destroy(currentAvatar);
    }

    private void OnAvatarTypeChanged(PlayerCustoms prev, PlayerCustoms next) => LoadAvatar(next);

    /// <summary>
    /// Loads and customizes the avatar using the shared AvatarLoader utility,
    /// then performs PlayerVisual-specific setup (renderer caching for infection effect, name tag).
    /// </summary>
    private void LoadAvatar(PlayerCustoms customs)
    {
        currentAvatar = AvatarLoader.Load(customs, transform.position, parent: transform);

        renderers = currentAvatar.GetComponentsInChildren<Renderer>(true);

        if (!IsOwner)
            playerTag.text = customs.name.ToString();
        else
            playerTag.text = "";

        InitDefaults();
    }

    private void OnInfectionToggle(bool prev, bool next) => ToggleEffect(next);

    void InitDefaults() {
        foreach (Renderer r in renderers)
        {
            defaultMaterial.Add(r.name, r.material);
        }
    }

    void ToggleEffect(bool isInfected)
    {
        if (isInfected)
        {
            foreach (Renderer r in renderers)
            {
                r.material = holoMat;
            }

            if (btnObj != null)
            {
                btnObj.SetActive(true);
            }
        }
        else
        {
            foreach (Renderer r in renderers)
            {
                Material defaultMat;
                defaultMaterial.TryGetValue(r.name, out defaultMat);
                r.material = defaultMat;
            }

            if (btnObj != null)
            {
                btnObj.SetActive(true);
            }
        }
    }
}
