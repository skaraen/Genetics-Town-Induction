using UnityEngine;

/// <summary>
/// Defines the biological profile data for an avatar.
/// Attach to the root GameObject of each avatar prefab so all metadata
/// travels with the prefab and renderer references resolve automatically on instantiation.
/// The reason to use this approach over a scriptable object approach is because scriptable objects
/// cant hold references to children components of other prefabs saved on file
/// </summary>
public class AvatarProfileData : MonoBehaviour
{
    const int NUM_SUBSEQUENCES = 2;

    [Header("Identity")]
    [Tooltip("Unique avatar type ID (e.g., 1=Jellyfish, 2=Sheep, 3=Hummingbird, 4=Axolotl)")]
    public int id;

    [Tooltip("Display name of this avatar species")]
    public string avatarName;

    [Header("DNA Sequences")]
    [Tooltip("Primary DNA base sequence (e.g., AGTAAAGGAGAA...)")]
    public string baseSequence;

    [Tooltip("Name of the trait controlled by the base sequence (e.g., Glow, Eyesight)")]
    public string baseSequenceTraitName;

    [Tooltip("Two sub-sequences for distinct changeable traits")]
    public string[] subSequences = new string[NUM_SUBSEQUENCES];

    [Tooltip("Names of traits controlled by each sub-sequence (e.g., Head Shape, Tentacles)")]
    public string[] subSequencesTraitNames = new string[NUM_SUBSEQUENCES];

    [Tooltip("Color DNA sequence displayed in the UI ticker")]
    public string colorSequence;

    [Header("Color Configuration")]
    [Tooltip("Direct references to renderers that receive color. " +
             "Layout: [headVariant1, headVariant2, torsoVariant1, torsoVariant2]")]
    public Renderer[] colorTargetRenderers = new Renderer[2 * NUM_SUBSEQUENCES];

    [Tooltip("Hex color range pairs (min/max) for each color slider")]
    public string[] colorRanges = new string[2 * NUM_SUBSEQUENCES];

    [Tooltip("Material color property to set (e.g., _EmissionColor or _BaseColor)")]
    public string colorPropertyName = "_EmissionColor";

    [Tooltip("Per-slider mode: if true, that slider adjusts HSV Value (lightness); if false, it adjusts Hue")]
    public bool[] colorSliderUsesValue = new bool[NUM_SUBSEQUENCES];

    [Header("Display")]
    [Tooltip("Educational description of this avatar's biological trait")]
    [TextArea(3, 6)]
    public string info;

    [Tooltip("Y position offset when spawning this avatar in selection/display scenes")]
    public float spawnYOffset = -2f;
}
