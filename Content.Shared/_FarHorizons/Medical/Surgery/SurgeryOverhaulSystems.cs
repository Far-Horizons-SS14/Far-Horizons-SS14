using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Damage;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Systems;

public sealed partial class SurgeryOverhaulSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly SharedIdentitySystem _identity = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryAlterAppearanceComponent, SurgeryStepCompleteEvent>(OnAlterAppearanceComplete);
        SubscribeLocalEvent<HealDamageComponent, SurgeryStepCompleteEvent>(OnHealDamageComplete);
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
        DamageSpecifier BonusHeal = new();
        DamageSpecifier TotalHeal;
        if (StepProto.TryGetComponent<HealDamageComponent>(out var healComp))
        {
            foreach (var (key, value) in healComp.TechnologyModifier!)
                if (CheckForTech(key.Id) && ResearchModifier > value)
                    ResearchModifier = value;
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

    public bool TryCheckForTech(string name) => CheckForTech(name);
    private bool CheckForTech(string name)
    {
        var query = EntityQueryEnumerator<TechnologyDatabaseComponent>();
        var TechProto = _prototypes.Index<TechnologyPrototype>(name);
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_research.IsTechnologyUnlocked(uid, TechProto, comp))
                return true;
        }
        return false;
    }
}