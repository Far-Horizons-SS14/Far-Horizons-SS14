using Content.Client.Weapons.Ranged.Systems;

namespace Content.Client.Weapons.Ranged.Components;

[RegisterComponent, Access(typeof(GunSystem))]
public sealed partial class SpentAmmoVisualsComponent : Component
{
    /// <summary>
    /// Should we do "{_state}-spent" or just "spent"
    /// </summary>
    [DataField]
    public bool Suffix = true;

    [DataField("state")]
    public string State = "base";  // Far Horizons

    /// <summary>
    /// Starlight
    /// Is there a hidden layer that should be revealed when spent?
    /// </summary>
    [DataField("revealSpent")] public bool revealSpent = false;
}

public enum AmmoVisualLayers : byte
{
    Base,
    Tip,
    Spent,
}
