using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
//FarHorizons Start
using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Body;
using Content.Shared.Stunnable;
using Content.Shared.Medical.Healing;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Content.Shared.Damage.Components;
//FarHorizons End

namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public abstract partial class SharedSurgerySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HealingSystem _healing = default!;
    private void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepCompleteEvent>(OnStepComplete);
        SubscribeLocalEvent<SurgeryClearProgressComponent, SurgeryStepCompleteEvent>(OnClearProgressStep);
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepEvent>(OnStep);
        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryDoAfterEvent>(OnTargetDoAfter);

        SubscribeLocalEvent<SurgeryStepComponent, SurgeryCanPerformStepEvent>(OnCanPerformStep);
        Subs.BuiEvents<SurgeryTargetComponent>(SurgeryUIKey.Key, subs => subs.Event<SurgeryStepChosenBuiMsg>(OnSurgeryTargetStepChosen));
    }
    private void OnTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !IsSurgeryValid(ent, target, args.Surgery, args.Step, out var surgery, out var part, out var step) ||
            !PreviousStepsComplete(ent, part, surgery, args.Step) ||
            !CanPerformStep(args.User, ent, part.Comp.Category, step, false))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            Dirty(ent);
            if (args.Target.HasValue && TryComp<OrganComponent>(args.Target.Value, out var dirtyPart))
                Dirty(args.Target.Value, dirtyPart, Comp<MetaDataComponent>(args.Target.Value));
            return;
        }
        //Far Horizons Start
        if (args.DidSurgeryFail)
        {
            var stepProto = _prototypes.Index<EntityPrototype>(args.Step);
            if (stepProto.TryGetComponent<OnFailDamageComponent>(out var comp, _compFactory) && TryComp<OrganComponent>(args.Target, out var organComp))
            {
                _damageableSystem.TryChangeDamage(organComp.Body!.Value, comp.Damage!);
                TryEmoteWithChat(organComp.Body!.Value, comp.Emote);
            }
            _popup.PopupEntity("Because of a careless tool, your hand shook and you damaged your patient. You need to start this step all over again!", args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }
        else
        {
            var ev = new SurgeryStepEvent(args.User, ent, part, GetTools(args.User))
            {
                StepProto = args.Step,
                SurgeryProto = args.Surgery,
            };
            RaiseLocalEvent(step, ref ev);
            if (ev.IsCancelled) return;
            var evComplete = new SurgeryStepCompleteEvent(args.User, ent, part, GetTools(args.User))
            {
                StepProto = args.Step,
                SurgeryProto = args.Surgery,
                IsFinal = surgery.Comp.Steps[^1] == args.Step,
            };
            RaiseLocalEvent(step, ref evComplete);
            if (_entitySystem.TryGetSingleton(args.Step, out var stepEnt) && TryComp(stepEnt, out HealingComponent? healing) && TryComp(ent, out DamageableComponent? damage))
                args.Repeat = _healing.HasDamage((stepEnt, healing), (ent, damage));
        }
        //Far Horizons End
        RefreshUI(ent);
    }

    private void OnClearProgressStep(Entity<SurgeryClearProgressComponent> ent, ref SurgeryStepCompleteEvent args)
    {
        var progress = Comp<SurgeryProgressComponent>(args.Part);
        progress.CompletedSteps.Clear();
        progress.CompletedSurgeries.Clear();
        progress.ActiveRepeatableStep = default; //FarHorizons
    }
    //FarHorizons Start
    private void OnStepComplete(Entity<SurgeryStepComponent> ent, ref SurgeryStepCompleteEvent args)
    {
        if (TryComp<SurgeryClearProgressComponent>(ent, out _))
            return;

        if (!TryComp<SurgeryProgressComponent>(args.Part, out var progress))
        {
            progress = new SurgeryProgressComponent();
            AddComp(args.Part, progress);
        }

        var stepKey = $"{args.SurgeryProto}:{args.StepProto}";

        if (!ent.Comp.Repeatable)
        {
            if (!progress.CompletedSteps.Contains(stepKey))
                progress.CompletedSteps.Add(stepKey);
        }
        else
        {
            progress.ActiveRepeatableStep = stepKey;
        }

        if (progress.ActiveRepeatableStep is { } active && active != stepKey)
        {
            if (!progress.CompletedSteps.Contains(active))
                progress.CompletedSteps.Add(active);
            progress.ActiveRepeatableStep = default;
        }

        if (!progress.StartedSurgeries.Contains(args.SurgeryProto) && !args.IsFinal)
            progress.StartedSurgeries.Add(args.SurgeryProto);

        if (progress.StartedSurgeries.Contains(args.SurgeryProto) && args.IsFinal)
            progress.StartedSurgeries.Remove(args.SurgeryProto);

        if (args.IsFinal)
            progress.CompletedSurgeries.Add(args.SurgeryProto);
    }
    //FarHorizons End
    private void OnStep(Entity<SurgeryStepComponent> ent, ref SurgeryStepEvent args)
    {
        if(!_entitySystem.TryGetSingleton(args.StepProto, out var stepEnt)
            || !TryComp(stepEnt, out SurgeryStepComponent? stepComp)) return;

        foreach (var reg in (ent.Comp.Tools ?? []).Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default) return;
            if (reg.Component is OrganComponent &&
                ent.Comp.OrganCategory != null &&
                TryComp<OrganComponent>(tool, out var organ) &&
                organ.Category != ent.Comp.OrganCategory)
                return;
            
            var specificToolComp = EntityManager.GetComponents(tool)
                .OfType<ISurgeryToolComponent>();

            SoundSpecifier? endSound = null;
            foreach(var usedTool in specificToolComp)
            {  
                var requestedTool = stepComp.Tools?.FirstOrDefault().Key;
                if(requestedTool != null)
                    if(usedTool.ToolType.Contains(requestedTool))
                    {
                        endSound = usedTool.EndSound;
                    }
            }

            if (_net.IsServer && TryComp(tool, out SurgeryToolComponent? toolComp) && endSound != null)
                _audio.PlayPvs(endSound, tool);
            if (ent.Comp.ReagentId != null && _solutionContainerSystem.TryGetSolution(tool, "drink", out var solution))
                _solutionContainerSystem.RemoveReagent(solution.Value, new ReagentQuantity(ent.Comp.ReagentId, ent.Comp.ReagentQuantity));
        }

        foreach (var reg in (ent.Comp.Add ?? []).Values)
        {
            var compType = reg.Component.GetType();
            if (HasComp(args.Part, compType))
                continue;
            var newComp = _compFactory.GetComponent(compType);
            _serialization.CopyTo(reg.Component, ref newComp, notNullableOverride: true);
            AddComp(args.Part, newComp);
        }

        if (ent.Comp.BodyAdd != null)
            EntityManager.AddComponents(args.Body, ent.Comp.BodyAdd, false);

        foreach (var reg in (ent.Comp.Remove ?? []).Values)
            RemComp(args.Part, reg.Component.GetType());

        foreach (var reg in (ent.Comp.BodyRemove ?? []).Values)
            RemComp(args.Body, reg.Component.GetType());
    }

    private void OnCanPerformStep(Entity<SurgeryStepComponent> ent, ref SurgeryCanPerformStepEvent args)
    {
        if (HasComp<SurgeryOperatingTableConditionComponent>(ent)
            && (!TryComp(args.Body, out BuckleComponent? buckle) || !HasComp<OperatingTableComponent>(buckle.BuckledTo)
             || !HasComp<KnockedDownComponent>(args.Body))) // FarHorizons
        {
            args.Invalid = StepInvalidReason.NeedsOperatingTable;
            return;
        }

        RaiseLocalEvent(args.Body, ref args);

        if (args.Invalid != StepInvalidReason.None)
            return;

        if (_inventory.TryGetContainerSlotEnumerator(args.Body, out var enumerator, args.TargetSlots))
        {
            var items = 0f;
            var total = 0f;
            while (enumerator.MoveNext(out var con))
            {
                total++;
                if (con.ContainedEntity != null && !_tag.HasTag(con.ContainedEntity.Value, "SurgeryCompatibleArmor"))
                    items++;
            }

            if (items > 0)
            {
                args.Invalid = StepInvalidReason.Armor;
                args.Popup = $"You need to take off armor from patient to perform this step!";
                return;
            }
        }

        if (args.Invalid != StepInvalidReason.None || ent.Comp.Tools == null)
            return;

        foreach (var reg in ent.Comp.Tools.Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default)
            {
                args.Invalid = StepInvalidReason.MissingTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need {toolComp.ToolName} to perform this step!";

                return;
            }
            //Far Horizons Start
            if (reg.Component is OrganComponent &&
                ent.Comp.OrganCategory != null &&
                TryComp<OrganComponent>(tool, out var organ) &&
                organ.Category != ent.Comp.OrganCategory)
            {
                args.Invalid = StepInvalidReason.MissingTool;
                return;
            }

            if (_hands.GetActiveItem(args.User) != tool && !_tag.HasTag(tool, "CyberHandItem"))
            {
                args.Invalid = StepInvalidReason.MissingTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need {toolComp.ToolName} on your main hand to perform this step!";
                return;
            }
            //Far Horizons End

            if (TryComp<ItemToggleComponent>(tool, out var togglable) && !togglable.Activated)
            {
                args.Invalid = StepInvalidReason.DisabledTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need enable {toolComp.ToolName} to perform this step!";

                return;
            }

            if (TryComp<SurgeryItemSizeConditionComponent>(ent, out var itemSizeComp) && TryComp<ItemComponent>(tool, out var item) && _item.GetSizePrototype(item.Size) > _item.GetSizePrototype(itemSizeComp.Size))
            {
                args.Invalid = StepInvalidReason.TooHigh;
                return;
            }

            if (ent.Comp.ReagentId != null && _solutionContainerSystem.GetTotalPrototypeQuantity(tool, ent.Comp.ReagentId) < ent.Comp.ReagentQuantity)
            {
                args.Invalid = StepInvalidReason.NotEnoughReagent;
                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need at least {ent.Comp.ReagentQuantity}u of {ent.Comp.ReagentId} in {toolComp.ToolName} to perform this step!";
                return;
            }

            args.ValidTools.Add(tool);
        }
    }

    private void OnSurgeryTargetStepChosen(Entity<SurgeryTargetComponent> ent, ref SurgeryStepChosenBuiMsg args)
    {
        var user = args.Actor;
        if (GetEntity(args.Entity) is not { Valid: true } body
            || GetEntity(args.Part) is not { Valid: true } targetPart
            || !IsSurgeryValid(body, targetPart, args.Surgery, args.Step, out var surgery, out var part, out var step)
            || !_entitySystem.TryGetSingleton(args.Step, out var stepEnt)
            || !TryComp(stepEnt, out SurgeryStepComponent? stepComp)
            || part.Comp.Category is not {} category
            || !CanPerformStep(user, body, category, step, true, out _, out _, out var validTools))
        {
            return;
        }
        if ((!PreviousStepsComplete(body, part, surgery, args.Step) || IsStepComplete(part, args.Surgery, args.Step)) && !(TryComp<SurgeryProgressComponent>(part, out var surgComp) 
        && surgComp.ActiveRepeatableStep == $"{args.Surgery}:{args.Step}")) //Far Horizons
        {
            var progress = Comp<SurgeryProgressComponent>(part);
            Dirty(part, progress);
            RefreshUI(ent);
            return;
        }

        if (_net.IsServer && TryComp(step, out MetaDataComponent? meta))
        {
            var surgeonName = MetaData(user).EntityName;
            _popup.PopupEntity($"{surgeonName.ToLower()} starts {meta.EntityName.ToLower()}", part, PopupType.LargeCaution);
        }

        var duration = stepComp.Duration;
        float SmallestSuccessRate = 1f;
        foreach (var tool in validTools)
            if (TryComp(tool, out SurgeryToolComponent? toolComp))
            {
                //FarHorizons Start
                var durationCap = duration * 2;
                var durationToSuccessRate = 0f;
                var bedSpeedMod = 2f;
                var toolSpeed = 1f;
                var toolSuccessRate = 1f;
                SoundSpecifier? startSound = null;
                var specificToolComp = EntityManager.GetComponents(tool)
                    .OfType<ISurgeryToolComponent>();

                foreach(var usedTool in specificToolComp)
                {
                    var requestedTool = stepComp.Tools?.FirstOrDefault().Key;
                    if(requestedTool != null)
                        if(usedTool.ToolType.Contains(requestedTool))
                        {
                            toolSpeed = usedTool.Speed;
                            toolSuccessRate = usedTool.SuccessRate;
                            startSound = usedTool.StartSound;
                        }
                }
                    
                if (TryComp(body, out BuckleComponent? buckle) && TryComp(buckle.BuckledTo, out SurgeryBedSpeedComponent? bedComp))
                    bedSpeedMod = bedComp.BedSpeedModifier;

                duration = duration * toolSpeed * bedSpeedMod;
                if (duration > durationCap)
                {
                    durationToSuccessRate = (float)Math.Clamp(Math.Pow(duration - durationCap, 2) / 100, 0.0, 0.75);
                    duration = durationCap;
                }
                if (startSound != null) _audio.PlayPvs(startSound, tool);

                var totalSuccesRate = Math.Clamp(toolSuccessRate - durationToSuccessRate, 0.25, 1);

                if (totalSuccesRate < SmallestSuccessRate)
                    SmallestSuccessRate = (float)totalSuccesRate;
                
            }
        bool didSurgeryFail = false;
        if (!_random.Prob(SmallestSuccessRate))
            didSurgeryFail = true;
        //FarHorizons End
        
        if (TryComp(body, out TransformComponent? xform))
            _rotateToFace.TryFaceCoordinates(user, _transform.GetMapCoordinates(body, xform).Position);

        var ev = new SurgeryDoAfterEvent(args.Surgery, args.Step, didSurgeryFail);
        var doAfter = new DoAfterArgs(EntityManager, user, duration, ev, body, part)
        {
            //FarHorizons Start
            DistanceThreshold = null, // it checks whether body part is accessible or not which it's not inside of its container. Have to set it to false here for a weird workaround
            NeedHand = true,
            BreakOnHandChange = true,
            //FarHorizons End
            BreakOnMove = true,
            //DuplicateCondition = DuplicateConditions.SameTarget,
            ForceNet = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    public (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, EntityUid surgery) => GetNextStep(body, part, surgery, []);
    private (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, Entity<SurgeryComponent?> surgery, List<EntityUid> requirements)
    {
        if (!Resolve(surgery, ref surgery.Comp))
            return null;

        if (requirements.Contains(surgery))
            throw new ArgumentException($"Surgery {surgery} has a requirement loop: {string.Join(", ", requirements)}");

        requirements.Add(surgery);

        if (surgery.Comp.Requirement is { } requirementsIds)
        {
            foreach (var requirementId in requirementsIds)
            {
                if (!_entitySystem.TryGetSingleton(requirementId, out var requirement)
                    && GetNextStep(body, part, requirement, requirements) is { } requiredNext
                    && IsSurgeryValid(body, part, requirementId, requiredNext.Surgery.Comp.Steps[requiredNext.Step], out _, out _, out _))
                    return requiredNext;
            }
        }

        if (!TryComp<SurgeryProgressComponent>(part, out var progress))
        {
            AddComp<SurgeryProgressComponent>(part);
            return ((surgery, surgery.Comp), 0);
        }
        var surgeryProto = Prototype(surgery);
        for (var i = 0; i < surgery.Comp.Steps.Count; i++)
            if (!progress.CompletedSteps.Contains($"{surgeryProto?.ID}:{surgery.Comp.Steps[i]}") &&
                !progress.ActiveRepeatableStep.Equals($"{surgeryProto?.ID}:{surgery.Comp.Steps[i]}")) //FarHorizons
                return ((surgery, surgery.Comp), i);

        return null;
    }

    public bool PreviousStepsComplete(EntityUid body, EntityUid part, Entity<SurgeryComponent> surgery, EntProtoId step)
    {
        if (surgery.Comp.Requirement is { } requirements)
        {
            foreach (var requirement in requirements)
            {
                if ((!_entitySystem.TryGetSingleton(requirement, out var requiredEnt)
                    || !TryComp(requiredEnt, out SurgeryComponent? requiredComp)
                    || !PreviousStepsComplete(body, part, (requiredEnt, requiredComp), step))
                    && IsSurgeryValid(body, part, requirement, step, out _, out _, out _))
                    return false;
            }
        }

        foreach (var surgeryStep in surgery.Comp.Steps)
        {
            if (surgeryStep == step)
                break;

            if (Prototype(surgery.Owner) is not EntityPrototype surgProto || !IsStepComplete(part, surgProto.ID, surgeryStep))
                return false;
        }

        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid body, ProtoId<OrganCategoryPrototype>? part, EntityUid step, bool doPopup) => CanPerformStep(user, body, part, step, doPopup, out _, out _, out _);
    public bool CanPerformStep(EntityUid user, EntityUid body, ProtoId<OrganCategoryPrototype>? part, EntityUid step, bool doPopup, out string? popup, out StepInvalidReason reason, out HashSet<EntityUid> validTools)
    {
        var slot = part.ToString() switch
        {
            "Head" => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES,
            "Torso" => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            "ArmLeft" or "ArmRight" => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            "HandLeft" or "HandRight" => SlotFlags.GLOVES,
            "LegLeft" or "LegRight" => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
            "FootLeft" or "FootRight" => SlotFlags.FEET,
            _ => SlotFlags.NONE
        };

        var check = new SurgeryCanPerformStepEvent(user, body, GetTools(user), slot);
        RaiseLocalEvent(step, ref check);
        popup = check.Popup;
        validTools = check.ValidTools;

        if (check.Invalid != StepInvalidReason.None)
        {
            if (doPopup && check.Popup != null)
                _popup.PopupEntity(check.Popup, user, PopupType.SmallCaution);

            reason = check.Invalid;
            return false;
        }

        reason = default;
        return true;
    }

    public bool IsStepComplete(EntityUid part, EntProtoId surgery, EntProtoId step)
    {
        if (TryComp<SurgeryProgressComponent>(part, out var comp))
        {
            //FarHorizons Start
            if (comp.ActiveRepeatableStep == $"{surgery}:{step}")
                return true;
            return comp.CompletedSteps.Contains($"{surgery}:{step}");
        }
        //FarHorizons End
        AddComp<SurgeryProgressComponent>(part);
        return false;
    }
    protected virtual void TryEmoteWithChat(EntityUid body, string? emote) { } // FarHorizons
 }