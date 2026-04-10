using System.Linq;
using Content.Server._Starlight.Language;
using Content.Shared._Starlight.Language.Components.Translators;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Radio.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared._Starlight.Cybernetics;
using Content.Shared._Starlight.Cybernetics.Components;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.VentCraw;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Starlight;

namespace Content.Server._Starlight.Medical.Surgery;
public sealed partial class OrganSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotInsertedEvent>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotRemovedEvent>(OnFunctionalOrganExtracted);

        SubscribeLocalEvent<LimbWithActionComponent, OrganGotInsertedEvent>(OnLimbwithActionOrganImplanted);
        SubscribeLocalEvent<LimbWithActionComponent, OrganGotRemovedEvent>(OnLimbwithActionOrganExtracted);

        SubscribeLocalEvent<StorageOrganComponent, OrganGotInsertedEvent>(OnStorageOrganImplanted);
        SubscribeLocalEvent<StorageOrganComponent, OrganGotRemovedEvent>(OnStorageOrganExtracted);

        SubscribeLocalEvent<OrganTongueComponent, OrganGotInsertedEvent>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, OrganGotRemovedEvent>(OnTongueExtracted);

        SubscribeLocalEvent<AbductorOrganComponent, OrganGotInsertedEvent>(OnAbductorOrganImplanted);
        SubscribeLocalEvent<AbductorOrganComponent, OrganGotRemovedEvent>(OnAbductorOrganExtracted);

        SubscribeLocalEvent<OrganDamageComponent, OrganGotInsertedEvent>(OnOrganImplanted);
        SubscribeLocalEvent<OrganDamageComponent, OrganGotRemovedEvent>(OnOrganExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
        {
            if (!EntityManager.HasComponent(args.Target, comp.Component.GetType()))
            {
                EntityManager.AddComponent(args.Target, comp.Component);
                UpdateEntity(args.Target, comp.Component, ent.Owner);
            }
        }
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
        {
            if (EntityManager.HasComponent(args.Target, comp.Component.GetType()))
            {
                EntityManager.RemoveComponent(args.Target, EntityManager.GetComponent(args.Target, comp.Component.GetType()));
                UpdateEntity(args.Target, comp.Component, ent.Owner);
            }
        }
    }

    private void UpdateEntity(EntityUid ent, IComponent comp, EntityUid? implant = null)
    {
        //For all those components where the enity needs to be updated in their own way after adding or removing a component
        switch (comp)
        {
            case IntrinsicTranslatorComponent _:
                _language.UpdateEntityLanguages(ent);
                break;
            case EncryptionKeyHolderComponent encrypt: //Move encryption keys between implant and body
                if(implant != null)
                    if(TryComp(implant, out EncryptionKeyHolderComponent? implantKeyHolder))
                        if (TryComp(ent, out EncryptionKeyHolderComponent? bodyKeyHolder))
                            foreach (var key in implantKeyHolder.KeyContainer.ContainedEntities.ToList())
                                _container.Insert(key, bodyKeyHolder.KeyContainer);
                        else
                            foreach (var key in encrypt.KeyContainer.ContainedEntities.ToList())
                                _container.Insert(key, implantKeyHolder.KeyContainer);
                break;
        }
    }

    private void OnLimbwithActionOrganImplanted(Entity<LimbWithActionComponent> ent, ref OrganGotInsertedEvent args)
    {
        var actionEntity = ent.Comp.ActionEntity;
        _action.AddAction(args.Target, ref actionEntity, ent.Comp.Action, ent.Owner);
        ent.Comp.ActionEntity = actionEntity;
    }

    private void OnLimbwithActionOrganExtracted(Entity<LimbWithActionComponent> ent, ref OrganGotRemovedEvent args) 
        => _action.RemoveAction(args.Target, ent.Comp.ActionEntity);

    private void OnStorageOrganImplanted(Entity<StorageOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        Dirty(ent);

        if (ent.Comp.OrganAction != null)
            _action.AddAction(args.Target, ref ent.Comp.ActionEntity, ent.Comp.OrganAction, ent.Owner);

        var limbComp = EnsureComp<PrivateStorageLimbComponent>(args.Target);
        limbComp.Limb.Add(ent.Owner);
    }

    private void OnStorageOrganExtracted(Entity<StorageOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        _action.RemoveAction(args.Target, ent.Comp.ActionEntity);
        ent.Comp.ActionEntity = null;

        if(TryComp<PrivateStorageLimbComponent>(args.Target, out var slComp))
        {
            slComp.Limb.Remove(ent.Owner);
            if(slComp.Limb.Count == 0)
                RemCompDeferred<PrivateStorageLimbComponent>(args.Target);
        }
    }

    private void OnOrganImplanted(Entity<OrganDamageComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<DamageableComponent>(args.Target, out var bodyDamageable)
            || ent.Comp.StoredDamage == null) return;

        _damageableSystem.ChangeDamage((args.Target, bodyDamageable), ent.Comp.StoredDamage.Invert(), true, false);
        ent.Comp.StoredDamage = null;
    }
    private void OnOrganExtracted(Entity<OrganDamageComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (ent.Comp.Damage is null
         || !TryComp<DamageableComponent>(args.Target, out var bodyDamageable)) return;

        ent.Comp.StoredDamage = _damageableSystem.ChangeDamage((args.Target, bodyDamageable), ent.Comp.Damage.Invert(), true, false);
    }

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
