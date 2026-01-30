using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System;

/// <summary>
/// Network-serializable representation of a player's visual customization settings 
/// that syncs player appearance and biological sequence settings
/// across the network. Makes consistent visual representation of players.
/// </summary>
[Serializable]
public struct PlayerCustoms: INetworkSerializable
{
    /// <summary>
    /// Identifier for the avatar model type (which 3D model to load)
    /// </summary>
    public int avatarType;

    /// <summary>
    /// Player's display name shown to other players in the game world.
    /// </summary>
    public FixedString64Bytes name;

    /// <summary>
    /// Controls visibility of the first biological subsequence (head/first part).
    /// </summary>
    public bool subSeq0Enabled;

    /// <summary>
    /// Controls visibility of the second biological subsequence (torso/second part).
    /// </summary>
    public bool subSeq1Enabled;

    /// <summary>
    /// Primary color applied to the first avatar part (typically head).
    /// Color property varies by avatar type (e.g., _BaseColor or _EmissionColor).
    /// </summary>
    public Color color0;

    /// <summary>
    /// Secondary color applied to the second avatar part (typically torso).
    /// Color property varies by avatar type (e.g., _BaseColor or _EmissionColor).
    /// </summary>
    public Color color1;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref avatarType);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref subSeq0Enabled);
        serializer.SerializeValue(ref subSeq1Enabled);
        serializer.SerializeValue(ref color0);
        serializer.SerializeValue(ref color1);
    }
}