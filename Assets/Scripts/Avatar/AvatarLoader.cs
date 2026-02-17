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
    /// Instantiates a fully customized avatar: loads the correct prefab from AvatarRegistry,
    /// toggles head/torso part variants, and applies colors via direct renderer references
    /// from the AvatarProfileData component on the instantiated prefab.
    /// </summary>
    /// <param name="customs">The player's visual customization data</param>
    /// <param name="position">World position to place the avatar</param>
    /// <param name="rotation">World rotation for the avatar</param>
    /// <param name="parent">Optional parent transform to attach the avatar to</param>
    /// <returns>The instantiated and customized avatar GameObject, or null on failure</returns>
    public static GameObject Load(PlayerCustoms customs, Vector3 position, Quaternion? rotation = null, Transform parent = null)
    {
        GameObject prefab = AvatarRegistry.Instance.GetPrefabById(customs.avatarType);

        if (prefab == null)
        {
            Debug.LogError($"Avatar prefab not found for avatar type: {customs.avatarType}");
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
    /// Applies the two custom colors to the avatar's head and torso materials
    /// using the direct renderer references from AvatarProfileData.
    /// colorTargetRenderers layout: [headVar1, headVar2, torsoVar1, torsoVar2].
    /// Head variants receive color0, torso variants receive color1.
    /// </summary>
    private static void ApplyColors(GameObject avatar, PlayerCustoms customs)
    {
        AvatarProfileData profile = avatar.GetComponent<AvatarProfileData>();
        if (profile == null || profile.colorTargetRenderers == null) return;

        for (int i = 0; i < profile.colorTargetRenderers.Length; i++)
        {
            Color color = (i < 2) ? customs.color0 : customs.color1;
            SetMaterialColor(profile, profile.colorTargetRenderers[i], color);
        }
    }

    /// <summary>
    /// Sets the color property specified by the profile's colorPropertyName on a renderer's material.
    /// </summary>
    private static void SetMaterialColor(AvatarProfileData profile, Renderer renderer, Color color)
    {
        if (renderer == null) return;
        Material mat = renderer.material;
        if (mat.HasProperty(profile.colorPropertyName))
        {
            mat.SetColor(profile.colorPropertyName, color);
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
    /// Searches recursively through all descendants, not just immediate children.
    /// </summary>
    private static void TogglePart(GameObject avatar, string partName, bool enable)
    {
        Transform part = avatar.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == partName);
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
