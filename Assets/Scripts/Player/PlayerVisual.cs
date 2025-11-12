using System.Collections.Generic;
using System.Linq;
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

    private void LoadAvatar(PlayerCustoms customs)
    {
        string path = AvatarProfileManager.Instance.GetPathById(customs.avatarType);
        GameObject prefab = Resources.Load<GameObject>("Avatars/" + path);
        Quaternion rotation = prefab.transform.rotation;
        rotation.y = 180;
        currentAvatar = Instantiate(prefab, transform.position, rotation, transform);

        int activeHeadIndex = customs.subSeq0Enabled ? 1 : 0;
        int activeTorsoIndex = customs.subSeq1Enabled ? 1 : 0;
        ToggleParts("head", activeHeadIndex);
        ToggleParts("torso", activeTorsoIndex);

        string[] targets = AvatarProfileManager.Instance.GetColorTargetsById(customs.avatarType);
        renderers = currentAvatar.GetComponentsInChildren<Renderer>(true);
        Material headMat = renderers.FirstOrDefault(r => r.gameObject.name == targets[0]).material;
        Material torsoMat = renderers.FirstOrDefault(r => r.gameObject.name == targets[1]).material;

        SetMaterialColor(customs.avatarType, headMat, customs.color0);
        SetMaterialColor(customs.avatarType, torsoMat, customs.color1);

        if (!IsOwner)
            playerTag.text = customs.name.ToString();
        else
            playerTag.text = "";

        InitDefaults();
    }

    void SetMaterialColor(int avatarType, Material mat, Color color)
    {
        string propertyName = (avatarType == 2) ? "_BaseColor" : "_EmissionColor";
        if (mat.HasProperty(propertyName))
        {
            mat.SetColor(propertyName, color);
        }
    }

    void TogglePart(string partName, bool enable)
    {
        Transform part = currentAvatar.transform.Find(partName);
        if (part != null)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }
        }
    }

    void ToggleParts(string baseName, int activeIndex)
    {
        for (int i = 0; i < 2; i++)
        {
            string partName = $"{baseName}{i + 1}";
            TogglePart(partName, i == activeIndex);
        }
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
