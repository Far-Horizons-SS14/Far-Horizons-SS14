using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.LimbDamage;

public sealed partial class LimbDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitHitChance();
        InitDamage();
        InitArmor();
        InitEffect();
        InitInspect();

        SubscribeLocalEvent<LimbDamageableComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<DamageableLimbComponent, ComponentInit>(OnLimbInit);
    }

    private void OnBodyInit(Entity<LimbDamageableComponent> ent, ref ComponentInit args) =>
        ent.Comp.Body = EnsureComp<BodyComponent>(ent);

    private void OnLimbInit(Entity<DamageableLimbComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Damageable = EnsureComp<DamageableComponent>(ent);
        ent.Comp.Organ = EnsureComp<OrganComponent>(ent);
    }
}