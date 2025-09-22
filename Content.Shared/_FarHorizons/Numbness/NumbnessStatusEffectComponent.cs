using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Numbness;

/// <summary>
/// Exists for use as a status effect. Adds a shader to the client that scales with the effect duration.
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NumbnessStatusEffectComponent : Component
{
}