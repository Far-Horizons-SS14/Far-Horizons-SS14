using System.Linq;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Medical.Surgery;

public sealed partial class SurgerySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> _defaultLimb = "Torso";
    private readonly string _surgeryCompTag = "SurgeryCompatibleArmor";

    public void RunSurgery(EntityUid user, Entity<SurgeryTargetComponent> target, ProtoId<OrganCategoryPrototype> targetLimb, EntityUid? limbTarget, EntityUid? tool, SurgeryStepPrototype step)
    {

        ISurgeryToolComponent? toolComp = null;
        if (step.Tool != null &&
            tool != null &&
            GetRequiredTool(step, tool.Value) is {} foundTool)
            toolComp = foundTool;
        
        var time = GetSurgeryTime(step, target, toolComp);

        NetEntity? limbTargetNEnt = limbTarget == null ? null : GetNetEntity(limbTarget.Value);
        NetEntity? toolNEnt = tool == null ? null : GetNetEntity(tool.Value);

        if (step.Popup != null)
        {
            var limbName = Loc.GetString($"limb-category-limb-name-{targetLimb.Id.ToLower()}");
            var popupMessage = Loc.GetString(step.Popup.Value);

            var prefix = Loc.GetString("surgery-step-popup-prefix", ("user", Identity.Entity(user, EntityManager)));
            var selfPrefix = Loc.GetString("surgery-step-popup-self-prefix");

            var message = Loc.GetString("surgery-step-message",
                                        ("prefix", prefix),
                                        ("message", popupMessage),
                                        ("target", Identity.Entity(target, EntityManager)),
                                        ("limbName", limbName));
            
            var selfMessage = Loc.GetString("surgery-step-message",
                                            ("prefix", selfPrefix),
                                            ("message", popupMessage),
                                            ("target", Identity.Entity(target, EntityManager)),
                                            ("limbName", limbName));

            _popup.PopupPredicted(selfMessage, message, target, user, Popups.PopupType.MediumCaution);
        }

        if (toolComp != null &&
            toolComp.StartSound != null)
            _audio.PlayPredicted(toolComp.StartSound, target, user);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, time, new SurgeryStepDoAfterEvent(step.ID, limbTargetNEnt, toolNEnt), target)
            {
                Hidden = false,
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                RequireCanInteract = true,
                BreakOnDropItem = true,
                BreakOnHandChange = false, // Maybe set it to true later? I kinda like being able to prepare for next step before current one is done
            });
    }

    public bool IsSurgeryPossible(EntityUid user, Entity<SurgeryTargetComponent> target, EntityUid? limbTarget)
    {
        if (user == target.Owner)
            return false; // maybe in the future move it for each step to handle separately for some limited degree of self-surgery
        
        if (!HasComp<KnockedDownComponent>(target) &&
            (!TryComp<BuckleComponent>(target, out var buckle) || 
            buckle.BuckledTo == null))
            return false;
        
        if (!TryComp<InventoryComponent>(target, out var targetInv))
            return true; // No inventory = nothing to block surgery

        var checkSlots = target.Comp.BlockingSlots;
        if (limbTarget != null &&
            TryComp<SurgeryLimbTargetComponent>(limbTarget.Value, out var limbComp))
            checkSlots = limbComp.BlockingSlots;

        var slotFlags = checkSlots.Aggregate(SlotFlags.NONE, (acc, p) => acc | p);
        if (slotFlags == SlotFlags.NONE)
            return true; // Nothing to check
        
        if (!_inventory.TryGetContainerSlotEnumerator((target, targetInv), out var enumerator, slotFlags))
            return false;
        
        while (enumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity != null &&
                !_tag.HasTag(slot.ContainedEntity.Value, _surgeryCompTag))
                return false;
        }

        return true;
    }

    public TimeSpan GetSurgeryTime(SurgeryStepPrototype step, Entity<SurgeryTargetComponent> ent, ISurgeryToolComponent? tool)
    {
        var time = step.BaseTime;

        if (tool != null)
            time *= tool.Speed;
        
        if (TryComp<BuckleComponent>(ent, out var buckle) &&
            buckle.BuckledTo != null &&
            TryComp<SurgeryBedSpeedComponent>(buckle.BuckledTo.Value, out var tableSpeed))
            time *= tableSpeed.BedSpeedModifier;
        
        return time;
    }

    public Entity<OrganComponent, SurgeryLimbTargetComponent>? GetTargetLimb(Entity<BodyComponent?> ent, ProtoId<OrganCategoryPrototype> target)
    {
        if (target == _defaultLimb ||
            !Resolve(ent, ref ent.Comp) ||
            ent.Comp.Organs == null ||
            ent.Comp.Organs.Count == 0)
            return null;
        
        var targetLimb = ent.Comp.Organs.ContainedEntities
                         .Select(p => (Entity<OrganComponent?, SurgeryLimbTargetComponent?>)(p, CompOrNull<OrganComponent>(p), CompOrNull<SurgeryLimbTargetComponent>(p)))
                         .Where(p => p.Comp1 != null && p.Comp2 != null)
                         .FirstOrNull(p => p.Comp1!.Category == target);

        return targetLimb == null ||
            targetLimb.Value.Comp1 == null ||
            targetLimb.Value.Comp2 == null
            ? null
            : (Entity<OrganComponent, SurgeryLimbTargetComponent>?)(targetLimb.Value, targetLimb.Value.Comp1, targetLimb.Value.Comp2);
    }

    public List<SurgeryStepPrototype> GetAllSteps(Entity<SurgeryTargetComponent?> ent, ProtoId<OrganCategoryPrototype> target)
    {
        var steps = new List<SurgeryStepPrototype>();

        if (!Resolve(ent, ref ent.Comp))
            return steps;

        if (target == _defaultLimb)
        {
            foreach (var pack in ent.Comp.Steps)
            {
                if (!ProtoMan.TryIndex(pack, out var packProto))
                    continue;
                
                foreach (var step in packProto.Steps)
                {
                    if (!ProtoMan.TryIndex(step, out var stepProto))
                        continue;
                    
                    steps.Add(stepProto);
                }
            }

            return steps;
        }

        if (!TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null ||
            body.Organs.Count == 0)
            return steps;

        var targetLimb = GetTargetLimb((ent.Owner, body), target);
        
        if (targetLimb == null)
            return steps;
        
        foreach (var pack in targetLimb.Value.Comp2!.Steps)
        {
            if (!ProtoMan.TryIndex(pack, out var packProto))
                continue;
            
            foreach (var step in packProto.Steps)
            {
                if (!ProtoMan.TryIndex(step, out var stepProto))
                    continue;
                
                steps.Add(stepProto);
            }
        }

        return steps;
    }

    public List<SurgeryStepPrototype> Validate(List<SurgeryStepPrototype> steps, EntityUid target, EntityUid? limb, EntityUid? tool, bool hard = true)
    {
        var validated = new List<SurgeryStepPrototype>();

        foreach (var step in steps)
        {
            if (!hard &&
                !IsToolValid(step, tool))
                continue;

            var conditions = hard ? step.Conditions : step.SoftConditions;

            if (!conditions.All(p => p.Validate(EntityManager, _limbStatus, target, limb, tool)))
                continue;
            
            validated.Add(step);
        }

        return validated;
    }

    public bool IsToolValid(SurgeryStepPrototype step, EntityUid? tool)
    {
        if (step.Tool == null || step.Tool.Count == 0)
            return true;
        
        if (tool == null)
            return false;
        
        foreach (var reg in step.Tool.Values)
        {
            if (reg.Component is not ISurgeryToolComponent comp)
                continue;
            
            if (HasComp(tool.Value, reg.Component.GetType()))
                return true;
        }

        return false;
    }

    public ISurgeryToolComponent? GetRequiredTool(SurgeryStepPrototype step, EntityUid tool)
    {
        if (step.Tool == null)
            return null;

        foreach (var reg in step.Tool.Values)
        {
            if (reg.Component is not ISurgeryToolComponent comp)
                continue;
            
            if (TryComp(tool, reg.Component.GetType(), out var toolComp))
                return (ISurgeryToolComponent)toolComp;
        }

        return null;
    }
}