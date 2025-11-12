using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System;

[Serializable]
public struct PlayerCustoms: INetworkSerializable
{
    public int avatarType;
    public FixedString64Bytes name;
    public bool subSeq0Enabled;
    public bool subSeq1Enabled;
    public Color color0;
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