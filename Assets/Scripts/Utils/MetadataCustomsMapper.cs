using Fusion;

/// <summary>
/// Maps between PlayerMetadata (local, full avatar data) and PlayerCustoms (network-serializable, minimal).
/// Used to convert avatar data for network transmission and to reconstruct full metadata on the receiving end.
/// </summary>
public static class MetaDataCustomsMapper
{
    /// <summary>
    /// Converts local PlayerMetadata into a network-ready PlayerCustoms struct.
    /// </summary>
    public static PlayerCustoms ToNet(PlayerMetadata m)
    {
        NetworkString<_64> networkName = default;
        networkName.Set(m.name ?? string.Empty);

        return new PlayerCustoms
        {
            avatarType = m.avatarType,
            name = networkName,
            subSeq0Enabled = m.sequenceEnabled[0],
            subSeq1Enabled = m.sequenceEnabled[1],
            color0 = m.colors[0],
            color1 = m.colors[1],
        };
    }

    /// <summary>
    /// Reconstructs full PlayerMetadata from a network PlayerCustoms struct
    /// by looking up the avatar profile from AvatarRegistry.
    /// </summary>
    public static PlayerMetadata ToLocal(int id, PlayerCustoms n)
    {
        AvatarProfileData profile = AvatarRegistry.Instance.GetProfileById(n.avatarType);
        if (profile == null)
        {
            UnityEngine.Debug.LogError($"No avatar profile found for avatar type {n.avatarType}");
            return null;
        }
        var m = new PlayerMetadata(profile);
        m.id = id;
        m.name = n.name.Value;
        m.avatarType = n.avatarType;
        m.sequenceEnabled[0] = n.subSeq0Enabled;
        m.sequenceEnabled[1] = n.subSeq1Enabled;
        if (n.subSeq0Enabled)
        {
            m.subSequences[0] = "ATG" + profile.subSequences[0];
        }
        else
        {
            m.subSequences[0] = profile.subSequences[0] + "TAG";
        }
        if (n.subSeq1Enabled)
        {
            m.subSequences[1] = "ATG" + profile.subSequences[1];
        }
        else
        {
            m.subSequences[1] = profile.subSequences[1] + "TAG";
        }
        m.colors[0] = n.color0;
        m.colors[1] = n.color1;
        return m;
    }
}
