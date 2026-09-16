namespace Content.Shared._FarHorizons.PDA;

// Far Horizons: Restricts ringer-code Store access to PDAs that have received a traitor uplink.
/// <summary>
/// Marks a PDA that has received a traitor uplink and may access ringtone-linked stores.
/// </summary>
[RegisterComponent]
public sealed partial class RingerCapablePDAComponent : Component;
