using System.Linq;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.Body.Systems;
using Content.Shared.Traits.Assorted;
using Content.Shared.Bed.Sleep;
using Content.Server.Administration.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Atmos.Rotting;
using Content.Server.NPC.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC;
using Content.Server.StationEvents.Components;
using Content.Server.Mind;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Tag;
using Content.Shared.Damage.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.HTN;
using Content.Shared._Starlight.Language.Components;
using Content.Shared.Mind.Components;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
//  
//This file is already overloaded with responsibilities,
//it’s time to break its functionality into different systems.
//However, I don’t want to touch the official systems, so I need to come up with extensions for them.
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StarlightEntitySystem _entity = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedRottingSystem _rottingSystem = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private readonly string _vimTag = "VimPilot";

    public void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepBleedEffectComponent, SurgeryStepEvent>(OnStepBleedComplete);
        SubscribeLocalEvent<SurgeryClampBleedEffectComponent, SurgeryStepEvent>(OnStepClampBleedComplete);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepEmoteEffectComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);

        SubscribeLocalEvent<SurgeryStepOrganExtractComponent, SurgeryStepEvent>(OnStepOrganExtractComplete);
        SubscribeLocalEvent<SurgeryStepOrganInsertComponent, SurgeryStepEvent>(OnStepOrganInsertComplete);

        SubscribeLocalEvent<SurgeryRemoveAccentComponent, SurgeryStepEvent>(OnRemoveAccent);

    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityQueryEnumerator<IncisionOpenComponent>();
        while (query.MoveNext(out var uid, out var incision))
        {
            if (!TryComp<OrganComponent>(uid, out var organ) || organ.Body == null || _timing.CurTime < incision.NextUpdate) // Far Horizons - check if the organ is attached
                continue;
            
            incision.NextUpdate = _timing.CurTime + incision.UpdateInterval;
            
            _bloodstreamSystem.TryModifyBleedAmount(organ.Body.Value, 0.1f); // Far Horizons - apply bleed to body and not to a random dude holding the arm
        }
    }

    private void OnStepBleedComplete(Entity<SurgeryStepBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {      
        if (ent.Comp.Damage == null)
            return;
        var damage = ent.Comp.Damage;  
        if (ent.Comp.Damage is not null && TryComp<DamageableComponent>(args.Body, out var comp))
            _damageableSystem.TryChangeDamage(args.Body, damage);
    }

    private void OnStepClampBleedComplete(Entity<SurgeryClampBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
    }
    private void OnStepOrganInsertComplete(Entity<SurgeryStepOrganInsertComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var organId)
            || !TryComp<BodyComponent>(args.Body, out var bodyComp)
            || !HasComp<OrganComponent>(organId))
            return;

        if (bodyComp.Organs == null ||
            !_containers.CanInsert(organId, bodyComp.Organs))
        {
            args.IsCancelled = true;
            return;
        }

        _containers.Insert(organId, bodyComp.Organs);

        if (HasComp<BrainComponent>(organId))
        {
            CopyAndPaste<NPCRetaliationComponent>(organId, args.Body);
            CopyAndPaste<NpcFactionMemberComponent>(organId, args.Body);
            CopyAndPaste<GhostTakeoverAvailableComponent>(organId, args.Body);
            CopyAndPaste<GhostRoleComponent>(organId, args.Body);
            CopyAndPaste<ActiveNPCComponent>(organId, args.Body);
            CopyAndPaste<SentienceTargetComponent>(organId, args.Body);
            CopyAndPaste<HTNComponent>(organId, args.Body);
            CopyAndPaste<LanguageKnowledgeComponent>(organId, args.Body);
            CopyAndPaste<LanguageSpeakerComponent>(organId, args.Body);
            CopyAndPaste<ParacusiaComponent>(organId, args.Body);
            
            if(TryComp<MindContainerComponent>(organId, out var mind) && mind != null)
                _mind.MakeSentient(args.Body);
        }
    }

    private void OnStepOrganExtractComplete(Entity<SurgeryStepOrganExtractComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Slot == null || !TryComp<BodyComponent>(args.Body, out var body) || body.Organs == null)
            return;

        //Far Horizons Start
        var surgProto = _prototypes.Index<EntityPrototype>(args.SurgeryProto);
        if (surgProto.TryGetComponent<NecrosisSurgeryStepComponent>(out var surgComp, _compFactory))
            if (TryComp<RottingComponent>(args.Body, out var rotting) && TryComp<PerishableComponent>(args.Body, out var perishable))
            {
                long ResearchModifier = 50;
                if (surgProto.TryGetComponent<SurgeryTechnologyComponent>(out var techvar, _compFactory) && 
                    _surgeryOverhaul.TryGetConnectedResearchServer(args.Body, out var server))
                {
                    foreach (var (key, value) in techvar.TechnologyModifier!)
                    {
                        if (_fhResearch.IsFlagUnlocked((server.Value, server.Value.Comp), key) && ResearchModifier > value)
                            ResearchModifier = value;
                    }
                }

                var BonusRotRemoved = Math.Round((rotting.TotalRotTime.TotalSeconds + perishable.RotAccumulator.TotalSeconds) / ResearchModifier);
                _rottingSystem.ReduceAccumulator(args.Body, TimeSpan.FromSeconds(surgComp.time + BonusRotRemoved));
            }

        // Far Horizons start
        var organsToRemove = body.Organs.ContainedEntities
            .Select(p => TryComp<OrganComponent>(p, out var organ) ? (Entity<OrganComponent>?)(p, organ) : null)
            .Where(p => p != null)
            .Where(p => ent.Comp.Slot != null && ent.Comp.Slot == p!.Value.Comp.Category)
            .ToList();

        foreach (var organ in organsToRemove)
        {
            if (!_containers.CanRemove(organ!.Value.Owner, body.Organs)) continue;

            _containers.Remove(organ.Value.Owner, body.Organs);

            if (HasComp<BrainComponent>(organ))
            {
                CopyAndPaste<NPCRetaliationComponent>(args.Body, organ.Value);
                CopyAndPaste<NpcFactionMemberComponent>(args.Body, organ.Value);
                CopyAndPaste<GhostTakeoverAvailableComponent>(args.Body, organ.Value);
                CopyAndPaste<GhostRoleComponent>(args.Body, organ.Value);
                CopyAndPaste<ActiveNPCComponent>(args.Body, organ.Value);
                CopyAndPaste<SentienceTargetComponent>(args.Body, organ.Value);
                CopyAndPaste<HTNComponent>(args.Body, organ.Value);
                CopyAndPaste<LanguageKnowledgeComponent>(args.Body, organ.Value);
                CopyAndPaste<LanguageSpeakerComponent>(args.Body, organ.Value);
                CopyAndPaste<ParacusiaComponent>(args.Body, organ.Value);
            }

            return;
        }
    }
    private void CopyAndPaste<T>(EntityUid from, EntityUid to) where T : IComponent
    {
        if (!TryComp<T>(from, out var comp))
            return;

        CopyComp(from, to, comp);
        RemComp<T>(from);
    }
    // Far Horizons end

    private void OnRemoveAccent(Entity<SurgeryRemoveAccentComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var accent in _accents)
            if (HasComp(args.Body, accent))
                RemCompDeferred(args.Body, accent);
    }

    private void OnStepEmoteEffectComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
    {

        if (!HasComp<PainNumbnessStatusEffectComponent>(args.Body) && !HasComp<SleepingComponent>(args.Body))
            _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
        else
            _sleeping.TryWaking(args.Body); // If the patient sleeping without n2o or reagents, wake them up.
    }

    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Entity, xform.Coordinates);
    }
    //FarHorizons End
}
