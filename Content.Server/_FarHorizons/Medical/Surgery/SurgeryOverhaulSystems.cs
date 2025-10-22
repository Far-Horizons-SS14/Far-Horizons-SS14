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
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using System.Linq;
using Robust.Shared.Containers;
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
    [Dependency] private readonly SharedBodySystem _sharedBodySystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedRottingSystem _rottingSystem = default!;
    private readonly List<EntProtoId> _surgeriesForRotten = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryAlterAppearanceComponent, SurgeryStepCompleteEvent>(OnAlterAppearanceComplete);
        SubscribeLocalEvent<HealDamageComponent, SurgeryStepCompleteEvent>(OnHealDamageComplete);
        SubscribeLocalEvent<NecrosisSurgeryComponent, SurgeryStepCompleteEvent>(OnNecrosisComplete);

        LoadSurgeriesForRotten();
    }

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
                && TryComp(buckle.BuckledTo, out DeviceLinkSinkComponent? linkComp) &&
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
            EntProtoId? chosenSurgery;
            if (_surgeriesForRotten.Count == 0)
                break;
                
            while (true)
            {
                chosenSurgery = _random.PickAndTake(_surgeriesForRotten);
                var chosenSurgeryProto = _prototypes.Index<EntityPrototype>(chosenSurgery);
                if (!TryComp<BodyPartComponent>(args.Part, out var partComp))
                    return;

                if (chosenSurgeryProto.TryGetComponent<SurgeryOrganExistConditionComponent>(out var organComp, _componentFactory))
                {
                    var Organs = _sharedBodySystem.GetPartOrgans(args.Part, partComp);
                    var organType = organComp.Organ!.Values.First().Component.GetType();
                    var hasOrgan = false;
                    foreach (var organ in Organs)
                        if (HasComp(organ.Id, organType))
                        {
                            hasOrgan = true;
                            break;
                        }
                    if (!hasOrgan) continue;
                }
                if (chosenSurgeryProto.TryGetComponent<NecrosisSurgeryStepComponent>(out var necroSurgComp, _componentFactory) &&
                    TryComp<ContainerManagerComponent>(args.Part, out var container) && necroSurgComp.Target != "bodypart")
                    if (container.Containers.TryGetValue(necroSurgComp.Target, out var limb) && limb.ContainedEntities.Count == 0)
                        continue;

                if (chosenSurgeryProto.TryGetComponent<SurgerySpeciesConditionComponent>(out var speciesComp, _componentFactory) &&
                    TryComp<HumanoidAppearanceComponent>(args.Body, out var charComp))
                {
                    var blacklist = speciesComp.SpeciesBlacklist;
                    var whitelist = speciesComp.SpeciesWhitelist;
                    if (whitelist.Contains(charComp.Species))
                        break;
                    else if (blacklist.Count > 0 && !blacklist.Contains(charComp.Species))
                        break;
                }
            }
            surgComp.RequiredSurgeries.Add((EntProtoId)chosenSurgery!);
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