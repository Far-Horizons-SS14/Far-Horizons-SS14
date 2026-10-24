using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.HitscanDamageModifier;

/// <summary>
/// A <see cref="HitscanDamageModifier"/> for increasing the damage of gun's with hitscan, should be used very sparingly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanDamageModifierComponent : Component
{
    /// <summary>
    /// Additional damage added onto the base damage.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();
}