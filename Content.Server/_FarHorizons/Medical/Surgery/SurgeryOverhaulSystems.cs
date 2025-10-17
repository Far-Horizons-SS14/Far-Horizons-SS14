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
using Content.Server.Starlight.Medical.Surgery;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Containers;
using Content.Shared.Prototypes;

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
    [Dependency] private readonly SurgerySystem _surgeries = default!;
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
        var ResearchModifier = 75f;
        TechnologyDatabaseComponent? TechDatabase = new();
        DamageSpecifier BonusHeal = new();
        DamageSpecifier TotalHeal;
        if (StepProto.TryGetComponent<HealDamageComponent>(out var healComp))
        {
            if (TryComp(args.Body, out BuckleComponent? buckle) && TryComp(buckle.BuckledTo, out DeviceLinkSinkComponent? linkComp))
            {
                foreach (var source in linkComp.LinkedSources)
                {
                    if (TryComp(source, out TechnologyDatabaseComponent? techComp))
                    {
                        TechDatabase = techComp;
                        break;
                    }
                }
            }
            foreach (var (key, value) in healComp.TechnologyModifier!)
            {
                var TechProto = _prototypes.Index<TechnologyPrototype>(key.Id);
                if (_research.IsTechnologyUnlocked(uid, TechProto, TechDatabase) && ResearchModifier > value)
                    ResearchModifier = value;
            }
            if (TryComp<DamageableComponent>(args.Body, out var dmgComp))
            {
                foreach (var key in healComp.Heal!.DamageDict.Keys)
                {
                    BonusHeal.DamageDict.Add(key, dmgComp.TotalDamage / ResearchModifier);
                }
            }
            TotalHeal = healComp.Heal! + (-BonusHeal);
            _damageableSystem.TryChangeDamage(args.Body, TotalHeal);
        }
    }
    private void OnNecrosisComplete(EntityUid uid, NecrosisSurgeryComponent comp, ref SurgeryStepCompleteEvent args)
    {
        if (_net.IsClient) return;
        var Target = args.Body;
        List<EntityUid> TargetParts = new();
        if (TryComp(Target, out BodyComponent? bodyComp) &&
            TryComp(bodyComp.RootContainer.ContainedEntity, out BodyPartComponent? partComp) &&
            TryComp(bodyComp.RootContainer.ContainedEntity, out ContainerManagerComponent? contComp))
        {
            foreach (var limbProto in partComp.Children.Keys)
            {
                if (limbProto.Equals("head"))
                    continue;
                var properID = "body_part_slot_" + limbProto;
                var limb = contComp.Containers[properID].ContainedEntities;
                if (limb.Count < 0)
                    continue;
                TargetParts.Add(limb[0]);
            }
            foreach (var organProto in partComp.Organs.Keys)
            {
                if (organProto.Equals("cavity"))
                    continue;
                var properID = "body_organ_slot_" + organProto;
                var organ = contComp.Containers[properID].ContainedEntities;
                if (organ.Count < 0)
                    continue;
                TargetParts.Add(organ[0]);
            }
            foreach (var test in _surgeriesForRotten)
                Logger.Info($"{test}");
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