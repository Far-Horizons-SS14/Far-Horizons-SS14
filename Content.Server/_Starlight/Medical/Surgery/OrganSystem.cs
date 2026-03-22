using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.VentCraw;

namespace Content.Server._Starlight.Medical.Surgery;
public sealed partial class OrganSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotInsertedEvent>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotRemovedEvent>(OnFunctionalOrganExtracted);

        SubscribeLocalEvent<OrganTongueComponent, OrganGotInsertedEvent>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, OrganGotRemovedEvent>(OnTongueExtracted);

        SubscribeLocalEvent<AbductorOrganComponent, OrganGotInsertedEvent>(OnAbductorOrganImplanted);
        SubscribeLocalEvent<AbductorOrganComponent, OrganGotRemovedEvent>(OnAbductorOrganExtracted);

        SubscribeLocalEvent<DamageableComponent, OrganGotInsertedEvent>(OnOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, OrganGotRemovedEvent>(OnOrganExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (!EntityManager.HasComponent(args.Target, comp.Component.GetType()))
                EntityManager.AddComponent(args.Target, comp.Component);
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (EntityManager.HasComponent(args.Target, comp.Component.GetType()))
                EntityManager.RemoveComponent(args.Target, _compFactory.GetComponent(comp.Component.GetType()));
    }

    //

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<DamageableComponent>(args.Target, out var bodyDamageable)) return;

        var change = _damageableSystem.ChangeDamage((args.Target, bodyDamageable), _damageableSystem.GetPositiveDamage(ent), true, false);
        _damageableSystem.ChangeDamage(ent.AsNullable(), change.Invert(), true, false);
    }
    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.Damage is null
         || !TryComp<DamageableComponent>(args.Target, out var bodyDamageable)) return;

        var change = _damageableSystem.ChangeDamage((args.Target, bodyDamageable), damageRule.Damage.Invert(), true, false);
        _damageableSystem.ChangeDamage(ent.AsNullable(), change.Invert(), true, false);
    }

    //

    private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (TryComp<AbductorVictimComponent>(args.Target, out var victim))
            victim.Organ = ent.Comp.Organ;
        if (ent.Comp.Organ == AbductorOrganType.Vent)
            AddComp<VentCrawlerComponent>(args.Target);
    }
    private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (TryComp<AbductorVictimComponent>(args.Target, out var victim))
            if (victim.Organ == ent.Comp.Organ)
                victim.Organ = AbductorOrganType.None;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            RemComp<VentCrawlerComponent>(args.Target);
    }

    //

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (HasComp<AbductorComponent>(args.Target) || ent.Comp.IsMuted) return;

        if (HasComp<MutedComponent>(args.Target))
            RemComp<MutedComponent>(args.Target);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        ent.Comp.IsMuted = HasComp<MutedComponent>(args.Target);
        EnsureComp<MutedComponent>(args.Target);
    }
}
