/// <summary>
/// Static data carrier for passing context into the Part2 (DNA sequence editing) scene.
/// Set before loading Part2Scene; read by Part2GameManager on Start.
/// </summary>
public static class Part2DataHolder
{
    /// <summary>
    /// The avatar profile ID of the other player whose DNA the local player will edit.
    /// </summary>
    public static int TargetAvatarId;
}
