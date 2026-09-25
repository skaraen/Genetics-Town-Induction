using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Fusion-serializable representation of a player's avatar customization.
/// </summary>
[Serializable]
public struct PlayerCustoms : INetworkStruct
{
    public int avatarType;
    public NetworkString<_64> name;
    public NetworkBool subSeq0Enabled;
    public NetworkBool subSeq1Enabled;
    public Color color0;
    public Color color1;
}
