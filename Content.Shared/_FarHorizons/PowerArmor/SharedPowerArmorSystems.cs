
using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Repairable;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.PowerArmor;

public abstract partial class SharedPowerArmorSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem _container = default!;
    [Dependency] protected DamageableSystem _damageable = default!;
    [Dependency] protected SharedAppearanceSystem _appearance = default!;
    [Dependency] protected SharedInteractionSystem _interaction = default!;
    [Dependency] protected InventorySystem _inventory = default!;
    [Dependency] protected MovementSpeedModifierSystem _movement = default!;
    [Dependency] protected SharedPopupSystem _popUp = default!;
    [Dependency] protected SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected PowerCellSystem _powerCell = default!;
    [Dependency] protected SharedHandsSystem _hands = default!;
    [Dependency] protected AccessReaderSystem _access = default!;
    [Dependency] protected SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] protected ItemSlotsSystem _items = default!;
    [Dependency] protected IGameTiming _timing = default!;
    [Dependency] protected AlertsSystem _alerts = default!;
    [Dependency] protected SharedBatterySystem _battery = default!;
    [Dependency] protected SharedTransformSystem _transform = default!;
    [Dependency] protected ClothingSpeedModifierSystem _clothSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerArmorComponent, GetVerbsEvent<AlternativeVerb>>(OnEquipVerb);
        SubscribeLocalEvent<PowerArmorComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<PowerArmorComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<PowerArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamage);
        SubscribeLocalEvent<PowerArmorComponent, InventoryRelayedEvent<LimbDamageModifyEvent>>(OnLimbDamage);
        SubscribeLocalEvent<PowerArmorComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<PowerArmorComponent, UninstallArmorPartMessage>(OnUninstallMessage);
        SubscribeLocalEvent<PowerArmorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PowerArmorComponent, AttemptSimpleToolUseEvent>(OnToolAttempt);
        SubscribeLocalEvent<PowerArmorComponent, SimpleToolDoAfterEvent>(ToolDoAfterComplete);
        SubscribeLocalEvent<PowerArmorComponent, InstallPartDoAfter>(AfterInstallDoAfter);
        SubscribeLocalEvent<PowerArmorComponent, TogglePowerArmorMessage>(OnPowerToggle);
        SubscribeLocalEvent<PowerArmorComponent, PowerArmorUninstallModuleMessage>(OnModuleUninstalledMessage);
        SubscribeLocalEvent<PowerArmorComponent, PowerArmorToggleModuleMessage>(OnModuleToggleMessage);
        SubscribeLocalEvent<PowerArmorComponent, TogglePowerArmorModuleActionEvent>(OnPowerAmorModuleToggleAction);
        SubscribeLocalEvent<PowerArmorComponent, TogglePowerArmorMenu>(OnPowerArmorMenuToggle);
        SubscribeLocalEvent<PowerArmorComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<PowerArmorComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<PowerArmorComponent, InteractUsingEvent>(RefRelayPAPartsEvent);
        SubscribeLocalEvent<PowerArmorComponent, InventoryRelayedEvent<InteractUsingEvent>>(OnInteractUsing);

        SubscribeLocalEvent<PowerArmorPartComponent, EntGotInsertedIntoContainerMessage>(OnPartInserted);
        SubscribeLocalEvent<PowerArmorPartComponent, EntGotRemovedFromContainerMessage>(OnPartEjected);
        SubscribeLocalEvent<PowerArmorPartComponent, BreakageEventArgs>(OnPartBroken);
        SubscribeLocalEvent<PowerArmorPartComponent, RepairDoAfterEvent>(OnRepair);
        SubscribeLocalEvent<PowerArmorPartComponent, AfterInteractEvent>(OnInteractUsing);

        SubscribeLocalEvent<PowerArmorModuleComponent, EntGotInsertedIntoContainerMessage>(OnModuleInstalled);
        SubscribeLocalEvent<PowerArmorModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleUninstalled);
        SubscribeLocalEvent<PowerArmorModuleComponent, AfterInteractEvent>(OnModuleInteract);
        SubscribeLocalEvent<PowerArmorPartComponent, AccessibleOverrideEvent>(AccessibleOverride);
        SubscribeLocalEvent<PowerArmorPartComponent, InRangeOverrideEvent>(InRangeOverride);

        SubscribeLocalEvent<PowerArmorUserComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUp);
    }

    #region Power Armor
    private void OnEquipVerb(Entity<PowerArmorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanComplexInteract 
        || !_interaction.InRangeAndAccessible(args.User, args.Target)
        || _container.IsEntityInContainer(args.Target))
            return;    

        var user = args.User;
        AlternativeVerb equip = new()
        {
            Act = () => _inventory.TryEquip(user, ent.Owner, "outerClothing", checkDoafter: true),
            Text = Loc.GetString("power-armor-verb-equip"),
            Priority = 1
        };

        args.Verbs.Add(equip);
    }

    private void OnEquip(Entity<PowerArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if(!ent.Comp.IsPrimary) return;

        ent.Comp.Wearer = args.Wearer;
        Dirty(ent);

        if (_timing.ApplyingState)
            return;

        var wearerComp = EnsureComp<PowerArmorUserComponent>(args.Wearer);
        wearerComp.Wearing = ent.Owner;
        Dirty(args.Wearer, wearerComp);
    }

    private void OnUnequip(Entity<PowerArmorComponent> ent, ref ClothingGotUnequippedEvent args)
    {        
        if(!ent.Comp.IsPrimary) return;

        if(TryComp<PowerArmorUserComponent>(ent.Comp.Wearer, out var PAUComp))
        {
            if(_alerts.IsShowingAlert(ent.Comp.Wearer.Value, PAUComp.BatteryAlert))
                _alerts.ClearAlert(ent.Comp.Wearer.Value, PAUComp.BatteryAlert);
            if(_alerts.IsShowingAlert(ent.Comp.Wearer.Value, PAUComp.NoBatteryAlert))
                _alerts.ClearAlert(ent.Comp.Wearer.Value, PAUComp.NoBatteryAlert);

            RemCompDeferred<PowerArmorUserComponent>(ent.Comp.Wearer.Value);
        }

        ent.Comp.Wearer = null;
        Dirty(ent);
    }
    
    private void OnDamage(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!ent.Comp.Parts.TryGetValue(PowerArmorVisualLayers.Chest, out var part)
            || part == null
            || !TryComp<PowerArmorPartComponent>(part, out var papComp))
            return;

        var modifiers = papComp.Modifiers;
        if (papComp.isBroken)
            modifiers = papComp.BrokenModifiers;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers, args.Args.ArmorPenetration, args.Args.CanHeal);
        if (!papComp.isBroken)
            _damageable.ChangeDamage(part.Value, args.Args.Damage);
    }

    private void OnLimbDamage(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<LimbDamageModifyEvent> args)
    {
        var limb = args.Args.Target.Id;

        var partType = limb switch
        {
            "Head" => PowerArmorVisualLayers.Head,
            "ArmLeft" or "HandLeft" => PowerArmorVisualLayers.LArm,
            "ArmRight" or "HandRight" => PowerArmorVisualLayers.RArm,
            "LegLeft" or "FootLeft" => PowerArmorVisualLayers.LLeg,
            "LegRight" or "FootRight" => PowerArmorVisualLayers.RLeg,
            _ => (PowerArmorVisualLayers?) null
        };

        if (partType == null
            || !ent.Comp.Parts.TryGetValue(partType.Value, out var part)
            || part == null
            || !TryComp<PowerArmorPartComponent>(part, out var papComp))
            return;

        var modifiers = papComp.Modifiers;
        if (papComp.isBroken)
            modifiers = papComp.BrokenModifiers;
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers, args.Args.ArmorPenetration, args.Args.CanHeal);
        if (!papComp.isBroken)
            _damageable.ChangeDamage(part.Value, args.Args.Damage);
    }

    private void OnRefreshMoveSpeed(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!ent.Comp.IsPrimary)
            return;

        var total = ent.Comp.TotalSpeedModifier;
        if (TryComp<PowerArmorComponent>(ent.Comp.OtherHalf, out var otherComp))
            total -= 1.0f-otherComp.TotalSpeedModifier;

        args.Args.ModifySpeed(total);
    }

    private void OnUninstallMessage(Entity<PowerArmorComponent> ent, ref UninstallArmorPartMessage args)
    {
        var partUid = GetEntity(args.Part);
        ent.Comp.UninstallTarget = partUid;
        Dirty(ent);
    }

    private void OnExamine(Entity<PowerArmorComponent> ent, ref ExaminedEvent args)
    {
        if(ent.Comp.UninstallTarget == null) return;
        if(TryComp<WiresPanelComponent>(ent.Owner, out var wires) && !wires.Open)
            args.PushMarkup("Open maintenance panel first.");
        else
            args.PushMarkup("Use a crowbar to uninstall part.");
    }

    private void OnToolAttempt(Entity<PowerArmorComponent> ent, ref AttemptSimpleToolUseEvent args)
    {
        if(!TryComp<WiresPanelComponent>(ent.Owner, out var wire))
            return;

        if(ent.Comp.UninstallTarget != null || wire.Open)
            return;
            
        if(ent.Comp.UninstallTarget == null)
            _popUp.PopupClient("No uninstall target on this armor.", args.User);
        if(!wire.Open)
            _popUp.PopupClient("Open wire panel first.", args.User);

        args.Cancelled = true;
    }

    private void ToolDoAfterComplete(Entity<PowerArmorComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        var target = ent.Comp.UninstallTarget;
        if (target == null || !Exists(target)) return;

        if (_container.TryGetContainingContainer(target.Value, out var container))
            _container.Remove(target.Value, container, reparent: false, force: true);

         _transform.DropNextTo(target.Value, ent.Owner);

        ent.Comp.UninstallTarget = null;
        Dirty(ent);
    }
    private void AfterInstallDoAfter(Entity<PowerArmorComponent> ent, ref InstallPartDoAfter args)
    {
        var part = GetEntity(args.Part);
        if(!HasComp<PowerArmorPartComponent>(part)) return;
        if(args.PartType == PowerArmorVisualLayers.Head 
        && _container.TryGetContainer(ent.Comp.OtherHalf, "parts", out var headPartContainer)
        && TryComp<PowerArmorComponent>(ent.Comp.OtherHalf, out var paComp))
        {
            _container.InsertOrDrop(part, headPartContainer);
            paComp.Parts[args.PartType] = part;
            Dirty(ent.Comp.OtherHalf, paComp);
        }
        else if(_container.TryGetContainer(ent.Owner, "parts", out var partContainer))
        {
            _container.InsertOrDrop(part, partContainer);
            ent.Comp.Parts[args.PartType] = part;
            Dirty(ent);
        }
    }

    private void OnPowerToggle(Entity<PowerArmorComponent> ent, ref TogglePowerArmorMessage args)
    {
        if(!TryComp<PowerCellDrawComponent>(ent.Owner, out var drawComp)) return;

        if(!_access.IsAllowed(args.Actor, ent.Owner))
        {
            _popUp.PopupEntity("Unauthorized Access Detected.", ent.Owner, args.Actor, PopupType.SmallCaution);
            return;
        }

        _powerCell.SetDrawEnabled(ent.Owner, !drawComp.Enabled);
        ent.Comp.IsPowered = !ent.Comp.IsPowered;
        _clothSpeed.TrySetSpeedModifier(ent, args.Actor, ent.Comp.IsPowered ? ent.Comp.PoweredMovementSpeedModifier : ent.Comp.UnpoweredMovementSpeedModifier);
        var modules = ent.Comp.Modules.ToList(); 
        foreach(var module in modules)
        {
            if(!TryComp<PowerArmorModuleComponent>(module, out var moduleComp) || !moduleComp.canBeToggled)
                continue;
                
            if(ent.Comp.IsPowered && moduleComp.isEnabled)
                InstallModule((module, moduleComp), ent.Owner);
            else if(!ent.Comp.IsPowered)
                UninstallModule((module, moduleComp), ent.Owner);
        }

        Dirty(ent);
    }

    private void OnModuleUninstalledMessage(Entity<PowerArmorComponent> ent, ref PowerArmorUninstallModuleMessage args)
    {
        var module = GetEntity(args.Module);
        if(!HasComp<PowerArmorModuleComponent>(module)) return;
        if(TryComp<WiresPanelComponent>(ent.Owner, out var wireComp) && !wireComp.Open)
        {
            _popUp.PopupEntity("Maintenance Panel is currently closed.", ent.Owner, args.Actor, PopupType.SmallCaution);
            return;
        }
        if(!_access.IsAllowed(args.Actor, ent.Owner))
        {
            _popUp.PopupEntity("Unauthorized Access Detected.", ent.Owner, args.Actor, PopupType.SmallCaution);
            return;
        }

        _container.TryRemoveFromContainer(module, false, out _);
        _hands.TryPickupAnyHand(args.Actor, module);
    }

    private void OnModuleToggleMessage(Entity<PowerArmorComponent> ent, ref PowerArmorToggleModuleMessage args)
    {
        var module = GetEntity(args.Module);
        if (!TryComp<PowerArmorModuleComponent>(module, out var moduleComp)) return;
        if (!ent.Comp.IsPowered)
        {
            _popUp.PopupEntity("Turn on suit before toggling modules.", ent.Owner, args.Actor, PopupType.SmallCaution);
            return;
        }

        if (moduleComp.isEnabled)
        {
            UninstallModule((module, moduleComp), ent.Owner);
            moduleComp.isEnabled = false;
        }
        else
        {
            moduleComp.isEnabled = true;
            InstallModule((module, moduleComp), ent.Owner);
        }
    }

    private void OnPowerAmorModuleToggleAction(Entity<PowerArmorComponent> ent, ref TogglePowerArmorModuleActionEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), PowerArmorRadialMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), PowerArmorRadialMenuUiKey.Key, args.Performer);
    }

    private void OnPowerArmorMenuToggle(Entity<PowerArmorComponent> ent, ref TogglePowerArmorMenu args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), PowerArmorMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), PowerArmorMenuUiKey.Key, args.Performer);
    }

    private void OnItemSlotEjectAttempt(Entity<PowerArmorComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<PowerCellSlotComponent>(ent, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_items.TryGetSlot(ent, cellSlotComp.CellSlotId, out var cellSlot) ||
            cellSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }

    private void OnItemSlotInsertAttempt(Entity<PowerArmorComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<PowerCellSlotComponent>(ent, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_items.TryGetSlot(ent, cellSlotComp.CellSlotId, out var cellSlot) ||
            cellSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }

    protected void RefRelayPAPartsEvent<T>(EntityUid uid, PowerArmorComponent component, ref T args) where T : IPowerArmorRelayedEvent
        => RelayEvent((uid, component), ref args);

    public void RelayEvent<T>(Entity<PowerArmorComponent> powerArmor, ref T args) where T : IPowerArmorRelayedEvent
    {
        var ev = new PowerArmorRelayedEvent<T>(args, powerArmor.Owner);
        foreach (var part in powerArmor.Comp.Parts)
        {
            if(part.Value == null) continue;
            RaiseLocalEvent(part.Value.Value, ev);
        }

        if(TryComp<PowerArmorComponent>(powerArmor, out var hPAComp))
            foreach (var part in hPAComp.Parts)
            {
                if(part.Value == null) continue;

                RaiseLocalEvent(part.Value.Value, ev);
            }

        args = ev.Args;
    }

    private void OnInteractUsing(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<InteractUsingEvent> args)
    {
        if(!TryComp<PowerArmorComponent>(ent, out var PAComp))
            return;
        RelayEvent((ent.Owner, PAComp), ref args.Args);
    }

    #endregion
    #region Power Armor Parts

    protected virtual void OnPartInserted(Entity<PowerArmorPartComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        ent.Comp.AttachedTo = args.Container.Owner;
        
        if(!TryComp<PowerArmorComponent>(ent.Comp.AttachedTo, out var paComp)) return;
        paComp.TotalSpeedModifier = (float) Math.Round(paComp.TotalSpeedModifier - ent.Comp.SpeedModifier, 2);

        paComp.Parts[ent.Comp.PartType] = ent.Owner;

        if(paComp.Wearer == null) return;

        _movement.RefreshMovementSpeedModifiers(paComp.Wearer.Value);
    }

    protected virtual void OnPartEjected(Entity<PowerArmorPartComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if(!TryComp<PowerArmorComponent>(ent.Comp.AttachedTo, out var paComp)) return;
        paComp.TotalSpeedModifier = (float) Math.Round(paComp.TotalSpeedModifier + ent.Comp.SpeedModifier, 2);

        paComp.Parts[ent.Comp.PartType] = null;
        ent.Comp.AttachedTo = null;

        if(paComp.Wearer == null) return;

        _movement.RefreshMovementSpeedModifiers(paComp.Wearer.Value);
    }

    public void OnPartBroken(Entity<PowerArmorPartComponent> ent, ref BreakageEventArgs args)
    {
        if(!_container.TryGetContainingContainer(ent.Owner, out var container) 
        || !TryComp<PowerArmorComponent>(container.Owner, out var paComp)) return;

        paComp.TotalSpeedModifier = (float) Math.Round(paComp.TotalSpeedModifier + ent.Comp.SpeedModifier, 2);
        
        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.PowerArmor, GetNetEntity(container.Owner));
        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.Visible, false);
        ent.Comp.isBroken = true;
        Dirty(ent);

        if(paComp.Wearer == null) return;

        _movement.RefreshMovementSpeedModifiers(paComp.Wearer.Value);
    }

    private void OnRepair(Entity<PowerArmorPartComponent> ent, ref RepairDoAfterEvent args)
    {
        if(args.Cancelled) return;
        
        if(_container.TryGetContainingContainer(ent.Owner, out var container) 
        && TryComp<PowerArmorComponent>(container.Owner, out var paComp))
        {
            paComp.TotalSpeedModifier = (float) Math.Round(paComp.TotalSpeedModifier - ent.Comp.SpeedModifier, 2);
        
            _appearance.SetData(ent.Owner, PowerArmorPartVisuals.PowerArmor, GetNetEntity(container.Owner));
            _appearance.SetData(ent.Owner, PowerArmorPartVisuals.Visible, true);

            if(paComp.Wearer != null) 
                _movement.RefreshMovementSpeedModifiers(paComp.Wearer.Value);
        }

        ent.Comp.isBroken = false;
        Dirty(ent);
    }

    private void OnInteractUsing(Entity<PowerArmorPartComponent> ent, ref AfterInteractEvent args)
    {
        if(!TryComp<PowerArmorComponent>(args.Target, out var PAComp)
        || !PAComp.IsPrimary) 
            return;

        if(!_access.IsAllowed(args.User, ent.Owner))
        {
            _popUp.PopupEntity("Unauthorized Access Detected.", ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        foreach(var part in PAComp.Parts)
        {
            if(ent.Comp.PartType == part.Key && part.Value != null)
            {
                _popUp.PopupClient("This piece is already installed.", args.User);
                return;
            }
        }

        if(Exists(PAComp.OtherHalf))
        {
            if(TryComp<PowerArmorComponent>(PAComp.OtherHalf, out var PAComp2))
            {
                foreach(var part in PAComp2.Parts)
                {
                    if(ent.Comp.PartType == part.Key && part.Value != null)
                    {
                        _popUp.PopupClient("This piece is already installed.", args.User);
                        return;
                    }
                }
            }
        }

        var InstallDoAfter = new InstallPartDoAfter(ent.Comp.PartType , GetNetEntity(args.Used));
        var installDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallTime, InstallDoAfter, args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true
        };
        _doAfter.TryStartDoAfter(installDoAfter);
        args.Handled = true;   
    }

    private void InRangeOverride(Entity<PowerArmorPartComponent> ent, ref InRangeOverrideEvent args)
    {
        if(!_container.TryGetOuterContainer(args.Target, Transform(args.Target), out var container))
            return;
        args.InRange = _interaction.InRangeUnobstructed(args.User, container.Owner, args.Range, args.CollisionMask, args.Predicate, args.Popup, args.OverlapCheck);
        args.Handled = true;
    }

    private void AccessibleOverride(Entity<PowerArmorPartComponent> ent, ref AccessibleOverrideEvent args)
    {
        if(!_container.TryGetOuterContainer(args.Target, Transform(args.Target), out var container))
            return;
        args.Accessible = _interaction.IsAccessible(args.User, container.Owner);
        args.Handled = true;
    }

    #endregion
    #region Power Armor Modules
    private void OnModuleInstalled(Entity<PowerArmorModuleComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if(TerminatingOrDeleted(args.Entity) 
        || !TryComp<PowerArmorComponent>(args.Container.Owner, out var PAComp)
        || !TryComp<PowerCellDrawComponent>(args.Container.Owner, out var drawComp)) 
            return;
        PAComp.Modules.Add(ent.Owner);
        Dirty(args.Container.Owner, PAComp);
        InstallModule(ent, args.Container.Owner);
        if(ent.Comp.IdlePowerDrain > 0)
            _powerCell.SetDrawRate(args.Container.Owner, drawComp.DrawRate + ent.Comp.IdlePowerDrain);
    }

    private void OnModuleUninstalled(Entity<PowerArmorModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if(TerminatingOrDeleted(args.Entity) 
        || !TryComp<PowerArmorComponent>(args.Container.Owner, out var PAComp)
        || !TryComp<PowerCellDrawComponent>(args.Container.Owner, out var drawComp)) 
            return;
        UninstallModule(ent, args.Container.Owner);
        if(PAComp.Modules.Contains(ent.Owner))
        {
            PAComp.Modules.Remove(ent.Owner);
            Dirty(args.Container.Owner, PAComp);
        }

         if(ent.Comp.IdlePowerDrain > 0)
            _powerCell.SetDrawRate(args.Container.Owner, drawComp.DrawRate - ent.Comp.IdlePowerDrain);

        if(ent.Comp.isEnabled) ent.Comp.isEnabled = false;
        Dirty(ent);
    }

    private void OnModuleInteract(Entity<PowerArmorModuleComponent> ent, ref AfterInteractEvent args)
    {

        if (args.Target is not { } powerArmor 
        || !TryComp<WiresPanelComponent>(powerArmor, out var wires) 
        || !TryComp<PowerArmorComponent>(powerArmor, out var paComp))
        {
            args.Handled = true;
            return;
        }

        if (!wires.Open)
        {
            _popUp.PopupEntity("Open maintenance panel first.", args.User);
            args.Handled = true;
            return;
        }

        var moduleMeta = MetaData(ent.Owner);
        var totalComplexity = 0;
        foreach (var module in paComp.Modules)
        {
            var otherProto = MetaData(module).EntityPrototype;
            if (!TryComp<PowerArmorModuleComponent>(module, out var pamComp) || otherProto == null)
                continue;

            if (otherProto.ID == moduleMeta.EntityPrototype?.ID)
            {
                _popUp.PopupEntity("This module is already installed.", args.User);
                args.Handled = true;
                return;
            }
            totalComplexity+=pamComp.ComplexityCost;
        }

        if(totalComplexity > paComp.MaxComplexity)
        {
            _popUp.PopupEntity("Unable to install due exceeding modding capacity.", args.User);
            args.Handled = true;
            return;
        }

        if (_container.TryGetContainer(powerArmor, "modules", out var moduleContainer))
        {
            _container.Insert(ent.Owner, moduleContainer);
            args.Handled = true;
        }
    }

    private void InstallModule(Entity<PowerArmorModuleComponent> module, EntityUid PowerArmor)
    {
        if(!TryComp<PowerCellDrawComponent>(PowerArmor, out var drawComp)
            || !TryComp<PowerArmorComponent>(PowerArmor, out var PAComp)) return;

        if(module.Comp.ActivePowerDrain > 0 && module.Comp.isEnabled)
            _powerCell.SetDrawRate(PowerArmor, drawComp.DrawRate + module.Comp.ActivePowerDrain);

        if (module.Comp.addToOtherHalf)
        {
            if (!Exists(PAComp.OtherHalf))
                return;
            PowerArmor = PAComp.OtherHalf;
        }

        if(TryComp<PowerArmorPassiveModuleComponent>(module.Owner, out var components) && components.Components != null)
            EntityManager.AddComponents(PowerArmor, components.Components);

        Dirty(module);
    }

    private void UninstallModule(Entity<PowerArmorModuleComponent> module, EntityUid PowerArmor)
    {
        if(!TryComp<PowerCellDrawComponent>(PowerArmor, out var drawComp)
            || !TryComp<PowerArmorComponent>(PowerArmor, out var PAComp)) return;

        if (module.Comp.ActivePowerDrain > 0 && module.Comp.isEnabled)
            _powerCell.SetDrawRate(PowerArmor, drawComp.DrawRate - module.Comp.ActivePowerDrain);

        if (module.Comp.addToOtherHalf)
        {
            if (!Exists(PAComp.OtherHalf))
                return;
            PowerArmor = PAComp.OtherHalf;
        }

        if (TryComp<PowerArmorPassiveModuleComponent>(module.Owner, out var components) && components.Components != null)
            EntityManager.RemoveComponents(PowerArmor, components.Components);

        Dirty(module);
    }
    #endregion

    private void OnGettingPickedUp(Entity<PowerArmorUserComponent> ent, ref GettingPickedUpAttemptEvent args)
        => args.Cancel();
}