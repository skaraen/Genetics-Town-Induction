/// <summary>
/// Static data carrier for passing context into the Part2 (DNA sequence editing) scene.
/// Set before loading Part2Scene; read by Part2GameManager on Start.
/// </summary>
public static class Part2DataHolder
{
    /// <summary>
    /// Full visual customization of the other player whose DNA the local player will edit.
    /// Contains avatar type, part toggles, and colors so the avatar renders with the same 
    /// customized effect that the other player has selected in the title screen.
    /// </summary>
    public static PlayerCustoms TargetCustoms;
}
