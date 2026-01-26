using System.Linq;
using UnityEngine;

/// <summary>
/// Reusable utility for instantiating and customizing an avatar from a PlayerCustoms struct.
/// Handles prefab loading, head/torso part toggling, and material color application.
/// Used by both PlayerVisual (GameScene) and Part2GameManager (Part2Scene).
/// </summary>
public static class AvatarLoader
{
    /// <summary>
    /// Instantiates a fully customized avatar: loads the correct prefab,
    /// toggles head/torso part variants, and applies colors to target materials.
    /// </summary>
    /// <param name="customs">The player's visual customization data</param>
    /// <param name="position">World position to place the avatar</param>
    /// <param name="rotation">World rotation for the avatar</param>
    /// <param name="parent">Optional parent transform to attach the avatar to</param>
    /// <returns>The instantiated and customized avatar GameObject, or null on failure</returns>
    public static GameObject Load(PlayerCustoms customs, Vector3 position, Quaternion? rotation = null, Transform parent = null)
    {
        string path = AvatarProfileManager.Instance.GetPathById(customs.avatarType);
        GameObject prefab = Resources.Load<GameObject>("Avatars/" + path);

        if (prefab == null)
        {
            Debug.LogError($"Avatar prefab not found at: Avatars/{path}");
            return null;
        }

        Quaternion finalRotation = rotation ?? prefab.transform.rotation;
        GameObject avatar = Object.Instantiate(prefab, position, finalRotation, parent);

        int activeHeadIndex = customs.subSeq0Enabled ? 1 : 0;
        int activeTorsoIndex = customs.subSeq1Enabled ? 1 : 0;
        ToggleParts(avatar, "head", activeHeadIndex);
        ToggleParts(avatar, "torso", activeTorsoIndex);

        ApplyColors(avatar, customs);

        return avatar;
    }

    /// <summary>
    /// Applies the two custom colors to the avatar's head and torso materials,
    /// using the color targets defined in the avatar's profile.
    /// </summary>
    private static void ApplyColors(GameObject avatar, PlayerCustoms customs)
    {
        string[] targets = AvatarProfileManager.Instance.GetColorTargetsById(customs.avatarType);
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);

        Renderer headRenderer = renderers.FirstOrDefault(r => r.gameObject.name == targets[0]);
        Renderer torsoRenderer = renderers.FirstOrDefault(r => r.gameObject.name == targets[1]);

        if (headRenderer != null)
            SetMaterialColor(customs.avatarType, headRenderer.material, customs.color0);
        if (torsoRenderer != null)
            SetMaterialColor(customs.avatarType, torsoRenderer.material, customs.color1);
    }

    /// <summary>
    /// Sets the appropriate color property on a material. Avatar type 2 uses _BaseColor;
    /// all others use _EmissionColor.
    /// </summary>
    private static void SetMaterialColor(int avatarType, Material mat, Color color)
    {
        string propertyName = (avatarType == 2) ? "_BaseColor" : "_EmissionColor";
        if (mat.HasProperty(propertyName))
        {
            mat.SetColor(propertyName, color);
        }
    }

    /// <summary>
    /// Toggles between two part variants (e.g., head1/head2) by enabling renderers
    /// on the active variant and disabling the other.
    /// </summary>
    private static void ToggleParts(GameObject avatar, string baseName, int activeIndex)
    {
        for (int i = 0; i < 2; i++)
        {
            string partName = $"{baseName}{i + 1}";
            TogglePart(avatar, partName, i == activeIndex);
        }
    }

    /// <summary>
    /// Enables or disables all renderers on a named child transform of the avatar.
    /// </summary>
    private static void TogglePart(GameObject avatar, string partName, bool enable)
    {
        Transform part = avatar.transform.Find(partName);
        if (part != null)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }
        }
    }
}
