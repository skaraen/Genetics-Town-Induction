using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all variant GameObjects for one DNA sequence slot.
/// Assign every variant (e.g., both head1 and head2) so that Part2Game
/// can grey them all out regardless of which variant is currently active.
/// </summary>
[Serializable]
public class SequenceBodyPartGroup
{
    [Tooltip("All variant GameObjects for this sequence (e.g., both head1 and head2). " +
             "All will be greyed out until the sequence is completed.")]
    public List<GameObject> bodyParts = new List<GameObject>();
}

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

    [Tooltip("Color range pairs (min/max) for each color slider")]
    public Color[] colorRanges = new Color[2 * NUM_SUBSEQUENCES];

    // Use this if you want to override whic material the color shifting changes.
    // Otherwise it defaults to the renderer's 0th material
    [Tooltip("Material index to use on each colorTargetRenderer. Defaults to 0 if unset.")]
    public int[] colorTargetMaterialIndices = new int[2 * NUM_SUBSEQUENCES];

    [Tooltip("Material color property to set (e.g., _EmissionColor or _BaseColor)")]
    public string colorPropertyName = "_EmissionColor";

    [Header("Sequence Body Parts")]
    // Index 0 = base sequence, index 1 = subSequences[0], index 2 = subSequences[1].
    // Include ALL variant GameObjects per slot (e.g., head1 AND head2 for the head sequence)
    // so that Part2Game can grey every variant regardless of which one the player selected.
    [Tooltip("One group per sequence, matched by index: [0] = base sequence, [1] = subSequences[0], [2] = subSequences[1]. " +
             "Add all variant GameObjects to each group.")]
    public List<SequenceBodyPartGroup> sequenceBodyParts = new List<SequenceBodyPartGroup>();

    [Header("Display")]
    [Tooltip("Educational description of this avatar's biological trait")]
    [TextArea(3, 6)]
    public string info;

    [Tooltip("Y position offset when spawning this avatar in selection/display scenes")]
    public float canvasYOffset = -2f;

    [Tooltip("Y position offset when spawning this avatar in 3D real world")]
    public float realWorldYOffset = -2f;

    [Tooltip("Y position offset when spawning this avatar in the Part2Game scene")]
    public float part2GameSpawnYOffset = 0f;
}
