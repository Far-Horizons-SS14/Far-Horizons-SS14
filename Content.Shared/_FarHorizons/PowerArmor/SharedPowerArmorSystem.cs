using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
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
        InitializeRelay();
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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
    
    [SubscribeLocalEvent]
    private void OnDamage(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!ent.Comp.Parts.TryGetValue(PowerArmorVisualLayers.Chest, out var part)
            || part == null
            || !TryComp<PowerArmorPartComponent>(part, out var papComp))
            return;

        var modifiers = papComp.isBroken ? papComp.BrokenModifiers : papComp.Modifiers;
        var modifiedDamage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers, args.Args.ArmorPenetration, args.Args.CanHeal);
        args.Args.Damage = papComp.isBroken ? modifiedDamage : modifiedDamage * papComp.DamageTransferModifier;
        if (!papComp.isBroken)
            _damageable.ChangeDamage(part.Value, modifiedDamage);
    }

    [SubscribeLocalEvent]
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

        var modifiers = papComp.isBroken ? papComp.BrokenModifiers : papComp.Modifiers;
        var modifiedDamage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers, args.Args.ArmorPenetration, args.Args.CanHeal);
        args.Args.Damage = papComp.isBroken ? modifiedDamage : modifiedDamage * papComp.DamageTransferModifier;
        if (!papComp.isBroken)
            _damageable.ChangeDamage(part.Value, modifiedDamage);
    }

    [SubscribeLocalEvent]
    private void OnRefreshMoveSpeed(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!ent.Comp.IsPrimary)
            return;

        var total = ent.Comp.TotalSpeedModifier;
        if (TryComp<PowerArmorComponent>(ent.Comp.OtherHalf, out var otherComp))
            total -= 1.0f-otherComp.TotalSpeedModifier;

        args.Args.ModifySpeed(total);
    }

    [SubscribeLocalEvent]
    private void OnUninstallMessage(Entity<PowerArmorComponent> ent, ref UninstallArmorPartMessage args)
    {
        var partUid = GetEntity(args.Part);
        ent.Comp.UninstallTarget = partUid;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<PowerArmorComponent> ent, ref ExaminedEvent args)
    {
        if(ent.Comp.UninstallTarget == null) return;
        if(TryComp<WiresPanelComponent>(ent.Owner, out var wires) && !wires.Open)
            args.PushMarkup("Open maintenance panel first.");
        else
            args.PushMarkup("Use a crowbar to uninstall part.");
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnPowerAmorModuleToggleAction(Entity<PowerArmorComponent> ent, ref TogglePowerArmorModuleActionEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), PowerArmorRadialMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), PowerArmorRadialMenuUiKey.Key, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnPowerArmorMenuToggle(Entity<PowerArmorComponent> ent, ref TogglePowerArmorMenu args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), PowerArmorMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), PowerArmorMenuUiKey.Key, args.Performer);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PowerArmorComponent> ent, ref InventoryRelayedEvent<InteractUsingEvent> args)
    {
        if(!TryComp<PowerArmorComponent>(ent, out var PAComp))
            return;
        RelayEvent((ent.Owner, PAComp), ref args.Args);
    }
}