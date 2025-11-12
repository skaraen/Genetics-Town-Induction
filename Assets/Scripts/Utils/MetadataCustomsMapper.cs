using Unity.Collections;

public static class MetaDataCustomsMapper
{
    public static PlayerCustoms ToNet(PlayerMetadata m)
    {
        return new PlayerCustoms
        {
            avatarType = m.avatarType,
            name = (FixedString64Bytes)m.name,
            subSeq0Enabled = m.sequenceEnabled[0],
            subSeq1Enabled = m.sequenceEnabled[1],
            color0 = m.colors[0],
            color1 = m.colors[1],
        };
    }

    public static PlayerMetadata ToLocal(int id, PlayerCustoms n)
    {
        AvatarProfile profile = AvatarProfileManager.Instance.GetProfileById(n.avatarType);
        var m = new PlayerMetadata(profile);
        m.id = id;
        m.name = n.name.ToString();
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