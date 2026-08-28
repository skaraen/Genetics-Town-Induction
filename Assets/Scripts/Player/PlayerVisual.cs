using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnAvatarCustomsChanged))]
    public PlayerCustoms AvatarCustoms { get; set; }

    [Networked, OnChangedRender(nameof(OnInfectionChanged))]
    public NetworkBool IsInfected { get; set; }

    [SerializeField] private TextMeshProUGUI playerTag;
    [SerializeField] private Material holoMat;

    private readonly Dictionary<string, Material> defaultMaterial = new();
    private Renderer[] renderers;
    private GameObject currentAvatar;

    public override void Spawned()
    {
        LoadAvatar(AvatarCustoms);
        ToggleEffect(IsInfected);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (currentAvatar != null)
            Destroy(currentAvatar);
    }

    public void SetAvatarCustoms(PlayerCustoms customs)
    {
        if (Object != null && Object.HasStateAuthority)
            AvatarCustoms = customs;

        LoadAvatar(customs);
    }

    public void SetInfected(bool infected)
    {
        if (Object != null && Object.HasStateAuthority)
            IsInfected = infected;

        ToggleEffect(infected);
    }

    private void OnAvatarCustomsChanged()
    {
        LoadAvatar(AvatarCustoms);
    }

    private void OnInfectionChanged()
    {
        ToggleEffect(IsInfected);
    }

    private void LoadAvatar(PlayerCustoms customs)
    {
        if (customs.avatarType == 0 || AvatarRegistry.Instance == null)
            return;

        if (currentAvatar != null)
            Destroy(currentAvatar);

        defaultMaterial.Clear();

        AvatarProfileData profileData = AvatarRegistry.Instance.GetProfileById(customs.avatarType);
        Vector3 adjustedPosition = new Vector3(transform.position.x, transform.position.y + profileData.realWorldYOffset, transform.position.z);
        currentAvatar = AvatarLoader.Load(customs, adjustedPosition, parent: transform);

        renderers = currentAvatar.GetComponentsInChildren<Renderer>(true);

        if (playerTag != null)
            playerTag.text = Object != null && Object.HasInputAuthority ? string.Empty : customs.name.Value;

        InitDefaults();
        ToggleEffect(IsInfected);
    }

    private void InitDefaults()
    {
        if (renderers == null)
            return;

        foreach (Renderer r in renderers)
            defaultMaterial[r.name] = r.material;
    }

    private void ToggleEffect(bool infected)
    {
        if (renderers == null)
            return;

        foreach (Renderer r in renderers)
        {
            if (infected)
            {
                r.material = holoMat;
                continue;
            }

            if (defaultMaterial.TryGetValue(r.name, out Material defaultMat))
                r.material = defaultMat;
        }
    }
}
