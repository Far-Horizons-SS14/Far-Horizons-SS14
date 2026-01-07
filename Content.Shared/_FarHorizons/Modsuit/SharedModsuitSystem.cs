using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Modsuit;

public sealed class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModsuitControlComponent, ComponentStartup>(OnControlStartup);
        SubscribeLocalEvent<ModsuitControlComponent, ComponentShutdown>(OnControlShutdown);
        SubscribeLocalEvent<ModsuitControlComponent, GotEquippedEvent>(OnControlEquipped);
        SubscribeLocalEvent<ModsuitControlComponent, GotUnequippedEvent>(OnControlUnequipped);
        SubscribeLocalEvent<ModsuitControlComponent, GetItemActionsEvent>(OnGetItemActions);

        // Actions
        SubscribeLocalEvent<ModsuitControlComponent, ModsuitDeployActionEvent>(OnDeployAction);
        SubscribeLocalEvent<ModsuitControlComponent, ModsuitActivateActionEvent>(OnActivateAction);
    }

    private void OnControlStartup(EntityUid uid, ModsuitControlComponent component, ComponentStartup args)
    {
        // Create containers for parts
        _container.EnsureContainer<Container>(uid, component.HelmetContainerId);
        _container.EnsureContainer<Container>(uid, component.ChestplateContainerId);
        _container.EnsureContainer<Container>(uid, component.GauntletsContainerId);
        _container.EnsureContainer<Container>(uid, component.BootsContainerId);

        // Spawn and store parts if prototypes are defined
        if (component.HelmetPrototype != null && component.Helmet == null)
        {
            var helmet = Spawn(component.HelmetPrototype, Transform(uid).Coordinates);
            var helmetComp = EnsureComp<ModsuitPartComponent>(helmet);
            helmetComp.Control = uid;
            helmetComp.PartType = ModsuitPartType.Helmet;
            _container.Insert(helmet, _container.EnsureContainer<Container>(uid, component.HelmetContainerId));
            component.Helmet = helmet;
        }

        if (component.ChestplatePrototype != null && component.Chestplate == null)
        {
            var chestplate = Spawn(component.ChestplatePrototype, Transform(uid).Coordinates);
            var chestplateComp = EnsureComp<ModsuitPartComponent>(chestplate);
            chestplateComp.Control = uid;
            chestplateComp.PartType = ModsuitPartType.Chestplate;
            _container.Insert(chestplate, _container.EnsureContainer<Container>(uid, component.ChestplateContainerId));
            component.Chestplate = chestplate;
        }

        if (component.GauntletsPrototype != null && component.Gauntlets == null)
        {
            var gauntlets = Spawn(component.GauntletsPrototype, Transform(uid).Coordinates);
            var gauntletsComp = EnsureComp<ModsuitPartComponent>(gauntlets);
            gauntletsComp.Control = uid;
            gauntletsComp.PartType = ModsuitPartType.Gauntlets;
            _container.Insert(gauntlets, _container.EnsureContainer<Container>(uid, component.GauntletsContainerId));
            component.Gauntlets = gauntlets;
        }

        if (component.BootsPrototype != null && component.Boots == null)
        {
            var boots = Spawn(component.BootsPrototype, Transform(uid).Coordinates);
            var bootsComp = EnsureComp<ModsuitPartComponent>(boots);
            bootsComp.Control = uid;
            bootsComp.PartType = ModsuitPartType.Boots;
            _container.Insert(boots, _container.EnsureContainer<Container>(uid, component.BootsContainerId));
            component.Boots = boots;
        }
    }

    private void OnControlShutdown(EntityUid uid, ModsuitControlComponent component, ComponentShutdown args)
    {
        // Actions are automatically cleaned up by the action system
    }

    private void OnControlEquipped(EntityUid uid, ModsuitControlComponent component, GotEquippedEvent args)
    {
        if (args.Slot != "back")
            return;

        component.Wearer = args.Equipee;

        // Apply inactive slowdown
        if (!component.Active)
            _movementSpeed.RefreshMovementSpeedModifiers(args.Equipee);

        Dirty(uid, component);
    }
    private void OnGetItemActions(EntityUid uid, ModsuitControlComponent component, GetItemActionsEvent args)
    {
        // Only grant actions when worn (not in hands)
        if (args.InHands)
            return;

        if (component.DeployAction != null)
            args.AddAction(ref component.DeployActionEntity, component.DeployAction);
        if (component.ActivateAction != null)
            args.AddAction(ref component.ActivateActionEntity, component.ActivateAction);
    }
    private void OnControlUnequipped(EntityUid uid, ModsuitControlComponent component, GotUnequippedEvent args)
    {
        if (args.Slot != "back")
            return;

        // Retract all parts
        if (component.Deployed)
            RetractAllParts(uid, component, args.Equipee);

        // Deactivate suit
        if (component.Active)
            SetActive(uid, component, false);

        component.Wearer = null;
        _movementSpeed.RefreshMovementSpeedModifiers(args.Equipee);

        Dirty(uid, component);
    }

    private void OnDeployAction(EntityUid uid, ModsuitControlComponent component, ModsuitDeployActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleDeployment(uid, component, args.Performer);
    }

    private void OnActivateAction(EntityUid uid, ModsuitControlComponent component, ModsuitActivateActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleActivation(uid, component, args.Performer);
    }

    public void ToggleDeployment(EntityUid uid, ModsuitControlComponent component, EntityUid user)
    {
        if (component.Activating)
        {
            _popup.PopupClient(Loc.GetString("modsuit-still-deploying"), uid, user);
            return;
        }

        if (component.Wearer == null)
        {
            _popup.PopupClient(Loc.GetString("modsuit-not-worn"), uid, user);
            return;
        }

        if (component.Deployed)
            RetractAllParts(uid, component, user);
        else
            DeployAllParts(uid, component, user);
    }

    public void ToggleActivation(EntityUid uid, ModsuitControlComponent component, EntityUid user)
    {
        if (!component.Deployed)
        {
            _popup.PopupClient(Loc.GetString("modsuit-not-deployed"), uid, user);
            return;
        }

        SetActive(uid, component, !component.Active, user);
    }

    private void DeployAllParts(EntityUid uid, ModsuitControlComponent component, EntityUid user)
    {
        if (component.Wearer == null || component.Activating)
            return;

        component.Activating = true;
        Dirty(uid, component);

        if (component.DeploySound != null)
            _audio.PlayPredicted(component.DeploySound, uid, user);

        // Deploy parts one by one
        DeployPart(uid, component, component.Helmet, "head", user);
        DeployPart(uid, component, component.Chestplate, "outerClothing", user);
        DeployPart(uid, component, component.Gauntlets, "gloves", user);
        DeployPart(uid, component, component.Boots, "shoes", user);

        component.Deployed = true;
        component.Activating = false;
        Dirty(uid, component);
    }

    private void DeployPart(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, string slot, EntityUid user)
    {
        if (partUid == null || component.Wearer == null)
            return;

        if (!_inventory.TryGetSlotEntity(component.Wearer.Value, slot, out var existingItem))
        {
            // Slot is empty, deploy the part
            if (_inventory.TryEquip(component.Wearer.Value, partUid.Value, slot, force: true))
            {
                if (TryComp<ModsuitPartComponent>(partUid.Value, out var partComp))
                {
                    partComp.Sealed = true;
                    
                    // Update appearance to show sealed state
                    if (TryComp<AppearanceComponent>(partUid.Value, out var appearance))
                        _appearance.SetData(partUid.Value, ToggleableVisuals.Enabled, true, appearance);
                    Dirty(partUid.Value, partComp);
                }
            }
        }
    }

    private void RetractAllParts(EntityUid uid, ModsuitControlComponent component, EntityUid user)
    {
        if (component.Wearer == null || component.Activating)
            return;

        component.Activating = true;
        Dirty(uid, component);

        if (component.DeploySound != null)
            _audio.PlayPredicted(component.DeploySound, uid, user);

        // Retract parts
        RetractPart(uid, component, component.Helmet, component.HelmetContainerId);
        RetractPart(uid, component, component.Chestplate, component.ChestplateContainerId);
        RetractPart(uid, component, component.Gauntlets, component.GauntletsContainerId);
        RetractPart(uid, component, component.Boots, component.BootsContainerId);

        component.Deployed = false;
        component.Activating = false;

        // Deactivate if active
        if (component.Active)
            SetActive(uid, component, false);

        Dirty(uid, component);
    }

    private void RetractPart(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, string containerId)
    {
        if (partUid == null || component.Wearer == null)
            return;

        // Update appearance to show unsealed state before unequipping
        if (TryComp<ModsuitPartComponent>(partUid.Value, out var partComp))
        {
            partComp.Sealed = false;
            Dirty(partUid.Value, partComp);
            
            // Update appearance to show unsealed state
            if (TryComp<AppearanceComponent>(partUid.Value, out var appearance))
                _appearance.SetData(partUid.Value, ToggleableVisuals.Enabled, false, appearance);
        }

        // Find which slot this part is in and unequip it
        if (_inventory.TryGetContainerSlotEnumerator(component.Wearer.Value, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (_inventory.TryGetSlotEntity(component.Wearer.Value, slot.ID, out var slotEntity) && slotEntity == partUid.Value)
                {
                    _inventory.TryUnequip(component.Wearer.Value, slot.ID, force: true);
                    break;
                }
            }
        }

        // Store in container
        var container = _container.EnsureContainer<Container>(uid, containerId);
        _container.Insert(partUid.Value, container);
    }

    public void SetActive(EntityUid uid, ModsuitControlComponent component, bool active, EntityUid? user = null)
    {
        if (component.Active == active)
            return;

        component.Active = active;

        if (active && component.ActivateSound != null && user != null)
            _audio.PlayPredicted(component.ActivateSound, uid, user.Value);
        else if (!active && component.DeactivateSound != null && user != null)
            _audio.PlayPredicted(component.DeactivateSound, uid, user.Value);

        // Update movement speed
        if (component.Wearer != null)
            _movementSpeed.RefreshMovementSpeedModifiers(component.Wearer.Value);

        Dirty(uid, component);
    }
}

public sealed partial class ModsuitDeployActionEvent : InstantActionEvent
{
}

public sealed partial class ModsuitActivateActionEvent : InstantActionEvent
{
}
