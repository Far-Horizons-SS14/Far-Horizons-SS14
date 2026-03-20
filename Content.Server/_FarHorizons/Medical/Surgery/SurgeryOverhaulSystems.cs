using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Damage;
using Content.Shared.Research.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Medical.Healing;
using Robust.Shared.Containers;
using Content.Shared.Tag;
using Content.Server._FarHorizons.Research;
using Content.Shared._FarHorizons.Research.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Body;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Medical.SurgeryOverhaul.Systems;

public sealed partial class SurgeryOverhaulSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly VisualBodySystem _visualBody = default!;
    [Dependency] private readonly HumanoidProfileSystem _profile = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly FHResearchSystem _fhResearch = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly StarlightEntitySystem _entity = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly SharedRottingSystem _rottingSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    private readonly List<EntProtoId> _surgeriesForRotten = [];
    private readonly string _organicTag = "Organic";
    private readonly string _inorganicTag = "Inorganic";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryAlterAppearanceComponent, SurgeryStepCompleteEvent>(OnAlterAppearanceComplete);
        SubscribeLocalEvent<HealingComponent, SurgeryStepCompleteEvent>(OnHealComplete);
        SubscribeLocalEvent<NecrosisSurgeryComponent, SurgeryStepCompleteEvent>(OnNecrosisComplete);
        SubscribeLocalEvent<SurgeryRepairEyesComponent, SurgeryStepCompleteEvent>(OnRepairEyesComplete);
        SubscribeLocalEvent<NecrosisSurgeryComponent, SurgeryValidEvent>(OnNecrosisSurgeryValid);
        SubscribeLocalEvent<NecrosisSurgeryStepComponent, SurgeryValidEvent>(OnNecrosisSurgeryStepValid);
        SubscribeLocalEvent<SurgeryTechnologyComponent, SurgeryValidEvent>(OnResearchSurgeryValid);
        SubscribeLocalEvent<SurgeryLimbExistConditionComponent, SurgeryValidEvent>(OnLimbExistConditionValid);
        SubscribeLocalEvent<RequireSpecificOrganicPartComponent, SurgeryValidEvent>(OnRequireSpecifiOrganicPartValid);
        SubscribeLocalEvent<RequireOrganicPartComponent, SurgeryValidEvent>(OnRequireOrganicPartValid);
        SubscribeLocalEvent<RequireInorganicPartComponent, SurgeryValidEvent>(OnRequireInorganicPartValid);

        SubscribeLocalEvent<SurgeryStepCustomOrganInsertComponent, SurgeryStepEvent>(OnStepCustomOrganInsertComplete);

        LoadSurgeriesForRotten();
    }

    private void OnStepCustomOrganInsertComplete(Entity<SurgeryStepCustomOrganInsertComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var item)
            || !TryComp<BodyComponent>(args.Body, out var bodyComp)
            || HasComp<OrganComponent>(item))
        {
            args.IsCancelled = true;
            return;
        }

        var comp = EnsureComp<OrganComponent>(item);
        comp.Category = ent.Comp.CustomCategory;
        EnsureComp<CustomOrganComponent>(item);

        if (bodyComp.Organs == null ||
            !_containers.CanInsert(item, bodyComp.Organs))
        {
            args.IsCancelled = true;
            return;
        }

        _containers.Insert(item, bodyComp.Organs);
    }

    public bool TryGetConnectedResearchServer
    (
        EntityUid body,
        [NotNullWhen(true)] out Entity<FHResearchTreeComponent>? server,
        BuckleComponent? buckle = null
    )
    {
        server = null;

        if (Resolve(body, ref buckle) &&
            TryComp(buckle.BuckledTo, out DeviceLinkSinkComponent? linkComp) &&
            linkComp.LinkedSources.Count != 0 &&
            linkComp.LinkedSources.First() is EntityUid clientEnt &&
            TryComp<ResearchClientComponent>(linkComp.LinkedSources.First(), out var clientComp) &&
            _fhResearch.TryGetServerWithTree((clientEnt, clientComp), out var treeServer))
            server = treeServer.Value;
        
        return server != null;
    }

//Surgeries
    private void OnAlterAppearanceComplete(EntityUid uid, SurgeryAlterAppearanceComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        var target = args.Body;

        if (!TryComp<HumanoidProfileComponent>(target, out var humanoid))
            return;

        if (_net.IsClient)
            return;

        var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _visualBody.ApplyProfileTo(target, newProfile);
        _profile.ApplyProfileTo(target, newProfile);
        _visualBody.MatchMarkingsToSkinColorAndRandomHair(target, newProfile);
        _metaData.SetEntityName(target, newProfile.Name, raiseEvents: false);
        _identity.QueueIdentityUpdate(target);
    }

    private void OnHealComplete(EntityUid uid, HealingComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        var StepProto = _prototypes.Index<EntityPrototype>(args.StepProto);
        var surgProto = _prototypes.Index<EntityPrototype>(args.SurgeryProto);
        var ResearchModifier = 75f;
        DamageSpecifier BonusHeal = new();
        DamageSpecifier TotalHeal;
        if (StepProto.TryGetComponent<HealingComponent>(out var healComp, _componentFactory))
        {
            if (surgProto.TryGetComponent<SurgeryTechnologyComponent> (out var techvar, _componentFactory) &&
                TryGetConnectedResearchServer(args.Body, out var server))
            {
                foreach (var (key, value) in techvar.TechnologyModifier!)
                {
                    if (_fhResearch.IsFlagUnlocked((server.Value, server.Value.Comp), key))
                        ResearchModifier = value;
                }
            }

            if (TryComp<DamageableComponent>(args.Body, out var dmgComp))
                foreach (var key in healComp.Damage!.DamageDict.Keys)
                    BonusHeal.DamageDict.Add(key, dmgComp.TotalDamage / ResearchModifier);

            TotalHeal = healComp.Damage! + (-BonusHeal);
            _damageableSystem.TryChangeDamage(args.Body, TotalHeal);
        }
    }
    private void OnNecrosisComplete(EntityUid uid, NecrosisSurgeryComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        LoadSurgeriesForRotten();

        if (!TryComp<NecrosisSurgeryTargetComponent>(args.Part, out var targetComp)) return;
        targetComp.RequiredSurgeries.Clear();

        for (int i = 0; i < targetComp.AmountOfSurgeries; i++)
        {
            if (_surgeriesForRotten.Count == 0)
                break;
            EntProtoId? chosenSurgery = null;

            while (_surgeriesForRotten.Count > 0)
            {
                chosenSurgery = _random.PickAndTake(_surgeriesForRotten);
                if (!_entity.TryGetSingleton(chosenSurgery.Value, out var surgeryEnt))
                    continue;
                var ev = new SurgeryValidEvent(args.Body, args.Part);

                RaiseLocalEvent(surgeryEnt, ref ev);

                if (!ev.Cancelled)
                    break;

                chosenSurgery = null;
            }
            if(chosenSurgery != null)
                targetComp.RequiredSurgeries.Add(chosenSurgery.Value);
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
        if (args.Cancelled) return;

        if (!_rottingSystem.IsRotten(args.Body))
        {
            args.Cancelled = true;
            return;
        }
    }
    private void OnNecrosisSurgeryStepValid(Entity<NecrosisSurgeryStepComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;

        if (!_rottingSystem.IsRotten(args.Body))
        {
            args.Cancelled = true;
            return;
        }

        var metaComp = MetaData(ent.Owner);
        if (HasComp<DisableSurgeryComponent>(ent.Owner)
            && TryComp<NecrosisSurgeryTargetComponent>(args.Part, out var necroComp)
            && metaComp.EntityPrototype != null
            && necroComp.RequiredSurgeries.Count >= necroComp.AmountOfSurgeries
            && !necroComp.RequiredSurgeries.Contains(metaComp.EntityPrototype.ID))
        {
            args.Cancelled = true;
            return;
        }
    }

    private void OnResearchSurgeryValid(Entity<SurgeryTechnologyComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;

        if (ent.Comp.RequiredTechnology != null)
        {
            if (!TryGetConnectedResearchServer(args.Body, out var server) || 
                !_fhResearch.IsFlagUnlocked((server.Value, server.Value.Comp), ent.Comp.RequiredTechnology.Value))
                args.Cancelled = true;
        }
    }

    private void OnLimbExistConditionValid(Entity<SurgeryLimbExistConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;

        args.Cancelled = !(TryComp<BodyComponent>(args.Body, out var bodyComp) &&
        bodyComp.Organs is not null &&
        bodyComp.Organs.ContainedEntities.Any(p => TryComp<OrganComponent>(p, out var organComp) && organComp.Category == ent.Comp.Slot));
    } 
    
    private void OnRequireSpecifiOrganicPartValid(Entity<RequireSpecificOrganicPartComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;
        
        if (TryComp<BodyComponent>(args.Body, out var bodyComp) && 
            bodyComp.Organs is not null &&
            bodyComp.Organs.ContainedEntities.Where(p => TryComp<OrganComponent>(p, out var organComp) && organComp.Category == ent.Comp.Slot).FirstOrNull() is {} targetOrgan)
            if (!_tag.HasTag(targetOrgan, _organicTag))
                args.Cancelled = true;
    } 

    private void OnRequireOrganicPartValid(Entity<RequireOrganicPartComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;
        
        if (!_tag.HasTag(args.Part, _organicTag))
            args.Cancelled = true;
    } 

    private void OnRequireInorganicPartValid(Entity<RequireInorganicPartComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled) return;
        
        if (!_tag.HasTag(args.Part, _inorganicTag))
            args.Cancelled = true;
    } 
            
    private void LoadSurgeriesForRotten()
    {
        _surgeriesForRotten.Clear();

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
            if (entity.HasComponent<NecrosisSurgeryStepComponent>())
                _surgeriesForRotten.Add(new EntProtoId(entity.ID));
    }
}