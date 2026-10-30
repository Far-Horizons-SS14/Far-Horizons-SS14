using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TazerComponent : Component
{
    [DataField]
    public SpriteSpecifier tazerLine =
    new SpriteSpecifier.Rsi(new ResPath("_FarHorizons/Objects/Weapons/Guns/Misc/taserline.rsi"), "taserline");

    [DataField]
    public DamageSpecifier DamagePerSecond = new();

    [ViewVariables]
    public List<EntityUid> CurrentProjectiles = new();

    [DataField]
    public float StaminaDrainRate = 20f;

    [DataField]
    public float MaxDistance = 10f;
}

/// <summary>
/// 
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TazerProjectileComponent : Component
{
    [DataField]
    public float CuttingTime = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TazedComponent: Component
{
   
    [DataField]
    public float UpdateTiming = 1f;

    [ViewVariables]
    public EntityUid User;

    [ViewVariables]
    public List<EntityUid> Sources = new();

    [ViewVariables]
    public TimeSpan NextUpdate;
    
}
