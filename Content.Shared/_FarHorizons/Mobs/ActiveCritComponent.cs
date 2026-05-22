using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Mobs;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCritComponent : Component
{
    [DataField] public TimeSpan CrawlDuration = TimeSpan.FromSeconds(10);

    [DataField] public int MinBlackoutSeconds = 0;
    [DataField] public int MaxBlackoutSeconds = 10;
    [DataField] public int MinAwakeSeconds = 10;
    [DataField] public int MaxAwakeSeconds = 20;

    [DataField] public float WhisperChance = 0.5f;
    [DataField] public float FallAfterStandingChance = 0.4f;
    [DataField] public float FallOnUseChance = 0.6f;
    [DataField] public DamageSpecifier DamageOnFall = new();

    [DataField] public float StandUpDoafterModifier = 5;

    [DataField] public float WalkSpeedModifier = 0.3f;
    [DataField] public float SprintSpeedModifier = 0.3f;

    [DataField] public bool RestrictCombat;
    [DataField] public bool RequireStandingForHands = true;

    [DataField] public float SpeechDistortionStrength = 0.5f;

    [DataField] public int AdjustTemporaryEyeDamage;

    [DataField] public LocId CantUseHandsMessage = "active-crit-cant-use-hands";
    [DataField] public LocId FailedStandUpMessage = "active-crit-standup-fail";

    [ViewVariables] public bool Blackout;
    [ViewVariables] public TimeSpan? BlackoutToggleAt;
}