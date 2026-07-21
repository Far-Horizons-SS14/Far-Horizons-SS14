using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Bosses.Elder;

[RegisterComponent]
public sealed partial class TelegraphElderBossHealingDecoyComponent : Component
{
    [DataField(required: true)] public EntProtoId Decoy;
    [DataField(required: true)] public int NumDecoys;
    [DataField(required: true)] public TimeSpan PassiveHealingRate;
    [DataField(required: true)] public DamageSpecifier PassiveHealing;
}

[RegisterComponent]
public sealed partial class ElderBossDecoyComponent : Component
{
    [ViewVariables] public EntityUid? Boss;
    [ViewVariables] public bool Exploding;
    [DataField(required: true)] public DamageSpecifier BossHealing;
    [DataField(required: true)] public EntProtoId ExplosionTelegraph;
}

[RegisterComponent]
public sealed partial class ElderBossDecoyRegenerationComponent : Component
{
    [ViewVariables] public List<EntityUid> Decoys;
    [ViewVariables] public TimeSpan NextUpdate;
    [DataField] public TimeSpan HealingRate = TimeSpan.FromSeconds(1);
    [DataField] public DamageSpecifier Healing = new();
}