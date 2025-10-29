using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Damage;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Content.Shared.Research.Prototypes;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;
using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Atmos.Rotting;

namespace Content.Server._FarHorizons.Medical.SurgeryOverhaul.Systems;

public sealed partial class SurgeryOverhaulSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly SharedIdentitySystem _identity = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly StarlightEntitySystem _entity = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly SharedRottingSystem _rottingSystem = default!;
    private readonly List<EntProtoId> _surgeriesForRotten = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryAlterAppearanceComponent, SurgeryStepCompleteEvent>(OnAlterAppearanceComplete);
        SubscribeLocalEvent<HealDamageComponent, SurgeryStepCompleteEvent>(OnHealDamageComplete);
        SubscribeLocalEvent<NecrosisSurgeryComponent, SurgeryStepCompleteEvent>(OnNecrosisComplete);
        SubscribeLocalEvent<SurgeryRepairEyesComponent, SurgeryStepCompleteEvent>(OnRepairEyesComplete);
        SubscribeLocalEvent<NecrosisSurgeryComponent, SurgeryValidEvent>(OnNecrosisSurgeryValid);
        SubscribeLocalEvent<SurgeryTechnologyComponent, SurgeryValidEvent>(OnResearchSurgeryValid);

        LoadSurgeriesForRotten();
    }
//Surgeries
    private void OnAlterAppearanceComplete(EntityUid uid, SurgeryAlterAppearanceComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        var target = args.Body;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        if (_net.IsClient)
            return;

        var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _humanoidAppearance.LoadProfile(target, newProfile, humanoid);
        _metaData.SetEntityName(target, newProfile.Name, raiseEvents: false);
        _identity.QueueIdentityUpdate(target);
    }

    private void OnHealDamageComplete(EntityUid uid, HealDamageComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        var StepProto = _prototypes.Index<EntityPrototype>(args.StepProto);
        var surgProto = _prototypes.Index<EntityPrototype>(args.SurgeryProto);
        var ResearchModifier = 75f;
        DamageSpecifier BonusHeal = new();
        DamageSpecifier TotalHeal;
        if (StepProto.TryGetComponent<HealDamageComponent>(out var healComp, _componentFactory))
        {
            if (surgProto.TryGetComponent<SurgeryTechnologyComponent> (out var techvar, _componentFactory) && TryComp(args.Body, out BuckleComponent? buckle)
                && TryComp(buckle.BuckledTo, out DeviceLinkSinkComponent? linkComp) && linkComp.LinkedSources.Count > 0 &&
                TryComp<TechnologyDatabaseComponent>(linkComp.LinkedSources.First(), out var techComp))
            {
                foreach (var (key, value) in techvar.TechnologyModifier!)
                {
                    var TechProto = _prototypes.Index<TechnologyPrototype>(key.Id);
                    if (_research.IsTechnologyUnlocked(uid, TechProto, techComp) && ResearchModifier > value)
                        ResearchModifier = value;
                }
            }

            if (TryComp<DamageableComponent>(args.Body, out var dmgComp))
                foreach (var key in healComp.Heal!.DamageDict.Keys)
                    BonusHeal.DamageDict.Add(key, dmgComp.TotalDamage / ResearchModifier);

            TotalHeal = healComp.Heal! + (-BonusHeal);
            _damageableSystem.TryChangeDamage(args.Body, TotalHeal);
        }
    }
    private void OnNecrosisComplete(EntityUid uid, NecrosisSurgeryComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        LoadSurgeriesForRotten();

        if (!TryComp<NecrosisSurgeryComponent>(args.Part, out var surgComp))
            return;

        var surgProto = _prototypes.Index<EntityPrototype>(args.SurgeryProto);
        if (!surgProto.TryGetComponent<NecrosisSurgeryComponent>(out var surgProtoComp, _componentFactory)) return;
        surgComp.RequiredSurgeries.Clear();

        for (int i = 0; i < surgProtoComp.AmountOfSurgeries; i++)
        {
            EntProtoId chosenSurgery;
            if (_surgeriesForRotten.Count == 0)
                break;

            while (true)
            {
                chosenSurgery = _random.PickAndTake(_surgeriesForRotten);
                if (!_entity.TryGetSingleton(chosenSurgery, out var surgeryEnt))
                    continue;
                var ev = new SurgeryValidEvent(args.Body, args.Part);

                RaiseLocalEvent(surgeryEnt, ref ev);

                if (!ev.Cancelled)
                    break;
            }
            surgComp.RequiredSurgeries.Add(chosenSurgery!);
        }
    }
    private void OnRepairEyesComplete(EntityUid uid, SurgeryRepairEyesComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (TryComp<BlindableComponent>(args.Body, out var blindComp))
            _blindableSystem.AdjustEyeDamage(args.Body, -blindComp.EyeDamage);
    }
    // Valid Event Checks
    private void OnNecrosisSurgeryValid(Entity<NecrosisSurgeryComponent> ent, ref SurgeryValidEvent args)
    {
        if (!_rottingSystem.IsRotten(args.Body))
        {
            args.Cancelled = true;
            return;
        }
        if (HasComp<DisableSurgeryComponent>(ent)
            && TryComp<MetaDataComponent>(ent, out var metaComp)
            && metaComp.EntityPrototype != null
            && !ent.Comp.RequiredSurgeries.Contains(metaComp.EntityPrototype.ID))
        {
            args.Cancelled = true;
            return;
        }
    }
    private void OnResearchSurgeryValid(Entity<SurgeryTechnologyComponent> ent, ref SurgeryValidEvent args)
    {
        if (ent.Comp.RequiredTechnology != null)
        {
            var TechProto = _prototypes.Index<TechnologyPrototype>(ent.Comp.RequiredTechnology);
            if (!TryComp(args.Body, out BuckleComponent? buckle) || !TryComp(buckle.BuckledTo, out DeviceLinkSinkComponent? linkComp) || linkComp.LinkedSources.Count == 0)
            {
                args.Cancelled = true;
                return;
            }
            if (TryComp(linkComp.LinkedSources.First(), out TechnologyDatabaseComponent? techComp) && !_research.IsTechnologyUnlocked(args.Body, TechProto, techComp))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
    private void LoadSurgeriesForRotten()
    {
        _surgeriesForRotten.Clear();

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            var surgProto = _prototypes.Index<EntityPrototype>(entity);
            if (surgProto.HasComponent<NecrosisSurgeryStepComponent>())
                _surgeriesForRotten.Add(new EntProtoId(entity.ID));
        }
    }
}