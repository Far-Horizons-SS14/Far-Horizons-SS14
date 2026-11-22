using System.Linq;
using Content.Server._FarHorizons.Silicons.Glitching;
using Content.Server.Hands.Systems;
using Content.Server.Stunnable;
using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Silicons.HumanoidEMP;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Silicons.HumanoidEMP;

public sealed partial class HumanoidEMPSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private StunSystem _stunSystem = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private GlitchingSystem _glitching = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidEMPComponent, EmpPulseEvent>(OnHumanoidEMP);
        SubscribeLocalEvent<OrganEmpTargetComponent, BodyRelayedEvent<GatherOrganEmpEffectEvent>>(OnOrganEmp);

        InitializeInspect();
    }

    private void OnOrganEmp(Entity<OrganEmpTargetComponent> ent, ref BodyRelayedEvent<GatherOrganEmpEffectEvent> args)
    {
        if (!TryComp<OrganComponent>(ent.Owner, out var organ) ||
            organ.Body == null || organ.Category == null)
            return;
        
        if (HasComp<DamageableLimbComponent>(ent))
            ApplyLimbDamage(organ.Body.Value, organ.Category.Value, ent.Comp.BaseDamage, args.Args.Strength);
        else
        {
            var categoryProto = _protoMan.Index(organ.Category);

            if (categoryProto.ConnectsTo == null || categoryProto.ConnectsTo == "Torso")
                ApplyDamage(organ.Body.Value, ent.Comp.BaseDamage, args.Args.Strength);
            else
                ApplyLimbDamage(organ.Body.Value, categoryProto.ConnectsTo.Value, ent.Comp.BaseDamage, args.Args.Strength);
        }

        args.Args = args.Args with { Effect = args.Args.Effect + ResolveThresholds(ent.Comp.Thresholds, args.Args.Strength) };
    }

    private void OnHumanoidEMP(Entity<HumanoidEMPComponent> ent, ref EmpPulseEvent args)
    {
        if (args.Disabled)
            return;
        
        if (_timing.CurTime < ent.Comp.NextEffect)
            return;
        
        ent.Comp.NextEffect = _timing.CurTime + ent.Comp.EffectCooldown;

        ApplyDamage(ent.Owner, ent.Comp.BaseDamage, args.Strength);

        var effect = ResolveThresholds(ent.Comp.Thresholds, args.Strength);
        var ev = new GatherOrganEmpEffectEvent(effect, args.Strength);
        RaiseLocalEvent(ent, ref ev);

        ApplyEffect(ent, ev.Effect);
    }

    public void ApplyEffect(EntityUid ent, HumanoidEMPEffect effect)
    {
        _stunSystem.TryKnockdown(ent, effect.KnockdownAmount, false, true, false, true);
        _stunSystem.TryAddStunDuration(ent, effect.StunAmount);
        foreach (var statusEffect in effect.AdditionalEffects)
            _status.TryAddStatusEffectDuration(ent, statusEffect.Key, out _, statusEffect.Value);
        
        _movementMod.TryAddMovementSpeedModDuration(ent, MovementModStatusSystem.FlashSlowdown, effect.SlowdownAmount, effect.WalkSpeedModifier, effect.SprintSpeedModifier);
        foreach (var hand in effect.DropItemsFrom)
            _hands.TryDrop(ent, hand, null, false, false);

        if (effect.GlitchDuration <= TimeSpan.Zero) return;
        var rampTime = effect.GlitchDuration / 4;
        _glitching.ApplyGlitch(ent, effect.GlitchDuration, rampTime);
    }

    public void ApplyDamage(Entity<DamageableComponent?> ent, DamageSpecifier baseDamage, int empStrength)
    {
        var scaledDmg = baseDamage * Math.Sqrt(empStrength);
        _damageable.TryChangeDamage(ent, scaledDmg, true);
    }

    public void ApplyLimbDamage(EntityUid body, ProtoId<OrganCategoryPrototype> target, DamageSpecifier baseDamage, int empStrength)
    {
        var scaledDmg = baseDamage * Math.Sqrt(empStrength);
        _limbDamage.TryChangeLimbDamage(body, target, scaledDmg, out _, true);
    }

    public static HumanoidEMPEffect ResolveThresholds(Dictionary<int, HumanoidEMPEffect> thresholds, int empStrength)
    {
        var res = new HumanoidEMPEffect();
        foreach (var (_, effect) in thresholds.Where(p => p.Key <= empStrength))
            res += effect;
        return res;
    }
        
}