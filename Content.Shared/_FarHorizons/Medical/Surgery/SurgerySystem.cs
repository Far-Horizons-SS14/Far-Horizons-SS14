using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private LimbStatusEffectSystem _limbStatus = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    private readonly VerbCategory _organicCategory = new("verb-categories-surgery", "/Textures/_FarHorizons/Interface/VerbIcons/scalpel.png");
    private readonly VerbCategory _inorganicCategory = new("verb-categories-inorganic-surgery", "/Textures/_FarHorizons/Interface/VerbIcons/screwdriver.png");

    [SubscribeLocalEvent]
    public void OnInteractedUsingTool(Entity<SurgeryTargetComponent> target, ref InteractUsingEvent args)
    {
        if (!TryComp<LimbTargettingComponent>(args.User, out var limbTarget))
            return;
        
        var targetLimb = GetTargetLimb(target.Owner, limbTarget.Target);

        if (!IsSurgeryPossible(args.User, target, targetLimb))
            return;

        var steps = GetAllSteps(target.AsNullable(), limbTarget.Target);
        var validSteps = Validate(steps, target, targetLimb, args.Used);
        var availableSteps = Validate(validSteps, target, targetLimb, args.Used, false);

        if (!availableSteps.Any())
            return;
        
        if (availableSteps.Count == 1)
        {
            RunSurgery(args.User, target, limbTarget.Target, targetLimb, args.Used, availableSteps.First());
            args.Handled = true;
            return;
        }

        var uiState = new SurgeryOptionBuiState(availableSteps.Select(p => (ProtoId<SurgeryStepPrototype>)p.ID).ToList(), GetNetEntity(target));
        _ui.OpenUi(args.User, SurgeryOptionUiKey.Key, true);
        _ui.SetUiState(args.User, SurgeryOptionUiKey.Key, uiState);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract ||
            !TryComp<SurgeryTargetComponent>(ev.Target, out var surgeryTarget) ||
            !TryComp<LimbTargettingComponent>(ev.User, out var limbTarget))
            return;
        
        var targetLimb = GetTargetLimb(ev.Target, limbTarget.Target);

        if (!IsSurgeryPossible(ev.User, (ev.Target, surgeryTarget), targetLimb))
            return;

        var steps = GetAllSteps((ev.Target, surgeryTarget), limbTarget.Target);
        var validSteps = Validate(steps, ev.Target, targetLimb, ev.Using);

        if (!validSteps.Any())
            return;

        var category = (targetLimb?.Comp2.SurgeryType ?? surgeryTarget.SurgeryType) switch
            {
                SurgeryStepType.Inorganic => _inorganicCategory,
                _ => _organicCategory,
            };
        
        var availableSteps = Validate(validSteps, ev.Target, targetLimb, ev.Using, false);
        var lockedSteps = validSteps.Where(p => !availableSteps.Contains(p)).ToList();

        foreach (var step in availableSteps)
        {
            var verb = new Verb
            {
                Text = step.Name,
                Category = category,
                Icon = step.Icon,
                Act = () => RunSurgery(ev.User, (ev.Target, surgeryTarget), limbTarget.Target, targetLimb, ev.Using, step),
            };

            ev.Verbs.Add(verb);
        }

        foreach (var lockedStep in lockedSteps)
        {
            var verb = new Verb
            {
                Text = lockedStep.Name,
                Category = category,
                Icon = lockedStep.Icon,
                Disabled = true
            };

            ev.Verbs.Add(verb);
        }
    }

    [SubscribeLocalEvent]
    private void OnSurgeryStepCompleted(Entity<SurgeryTargetComponent> ent, ref SurgeryStepDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        
        EntityUid? targetLimb = args.Limb == null ? null : GetEntity(args.Limb.Value);
        EntityUid? tool = args.ToolEnt == null ? null : GetEntity(args.ToolEnt.Value);
        
        var step = ProtoMan.Index(args.Step);

        if (_net.IsServer &&
            tool != null &&
            GetRequiredTool(step, tool.Value) is {} toolEnt &&
            toolEnt.EndSound != null)
            _audio.PlayPvs(toolEnt.EndSound, ent);

        foreach (var effect in step.Effects)
            effect.Run(EntityManager, _limbStatus, ent.Owner, targetLimb, tool);

        if (!step.Repeatable)
            return;
        
        var thisStep = new List<SurgeryStepPrototype>() { step };
        var validStep = Validate(thisStep, ent, targetLimb, tool);
        validStep = Validate(validStep, ent, targetLimb, tool, false);

        if (!validStep.Any())
            return;
        
        args.Repeat = true;
    }

    [SubscribeLocalEvent]
    private void OnOptionSelected(Entity<LimbTargettingComponent> ent, ref SurgeryStepSelectedMessage args)
    {
        var target = GetEntity(args.Target);

        if (!_interaction.InRangeAndAccessible(ent.Owner, target) ||
            !TryComp<SurgeryTargetComponent>(target, out var surgeryTarget))
            return;
        
        var targetLimb = GetTargetLimb(target, ent.Comp.Target);

        if (!IsSurgeryPossible(ent, (target, surgeryTarget), targetLimb))
            return;

        var steps = GetAllSteps((target, surgeryTarget), ent.Comp.Target);
        var tool = _hands.GetActiveItem(ent.Owner);
        var validSteps = Validate(steps, target, targetLimb, tool);
        validSteps = Validate(validSteps, target, targetLimb, tool, false);

        var chosenStep = args.Step;

        if (validSteps.FirstOrDefault(p => p.ID == chosenStep) is {} step)
            RunSurgery(ent, (target, surgeryTarget), ent.Comp.Target, targetLimb, tool, step);
    }
}