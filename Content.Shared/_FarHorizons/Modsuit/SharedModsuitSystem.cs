using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        // Deactivate suit first
        if (component.Active)
            SetActive(uid, component, false);

        // Remove unremoveable from control unit
        RemComp<UnremoveableComponent>(uid);

        // Retract all parts if deployed
        if (component.Deployed && component.Wearer != null)
        {
            // Force remove unremoveable from all parts before retracting
            if (component.Helmet != null && Exists(component.Helmet.Value))
                RemComp<UnremoveableComponent>(component.Helmet.Value);
            if (component.Chestplate != null && Exists(component.Chestplate.Value))
                RemComp<UnremoveableComponent>(component.Chestplate.Value);
            if (component.Gauntlets != null && Exists(component.Gauntlets.Value))
                RemComp<UnremoveableComponent>(component.Gauntlets.Value);
            if (component.Boots != null && Exists(component.Boots.Value))
                RemComp<UnremoveableComponent>(component.Boots.Value);
            
            RetractAllParts(uid, component, args.Equipee);
        }

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

        if (component.Active)
        {
            _popup.PopupClient(Loc.GetString("modsuit-deactivate-first"), uid, user);
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

        // If not primed and not active, prime the activation
        if (!component.ActivationPrimed && !component.Active)
        {
            component.ActivationPrimed = true;
            _popup.PopupClient(Loc.GetString("modsuit-activation-primed"), uid, user);
            UpdateActivateActionIcon(uid, component, true);
            Dirty(uid, component);
            return;
        }

        // If primed or already active, toggle activation
        component.ActivationPrimed = false;
        SetActive(uid, component, !component.Active, user);
        UpdateActivateActionIcon(uid, component, false);
    }

    private void DeployAllParts(EntityUid uid, ModsuitControlComponent component, EntityUid user)
    {
        if (component.Wearer == null || component.Activating)
            return;

        component.Activating = true;
        Dirty(uid, component);

        if (component.DeploySound != null)
            _audio.PlayGlobal(component.DeploySound, user, AudioParams.Default.WithVolume(-5f));

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

        // Ensure the part component link is maintained
        if (TryComp<ModsuitPartComponent>(partUid.Value, out var partComp))
        {
            partComp.Control = uid;
            partComp.Sealed = false; // Start unsealed when deployed
            Dirty(partUid.Value, partComp);
        }

        // Remove from container first if it's stored
        if (_container.TryGetContainingContainer(partUid.Value, out var container))
            _container.Remove(partUid.Value, container);

        if (!_inventory.TryGetSlotEntity(component.Wearer.Value, slot, out var existingItem))
        {
            // Slot is empty, deploy the part (silently to avoid spam)
            if (_inventory.TryEquip(component.Wearer.Value, partUid.Value, slot, silent: true, force: true))
            {
                // Update appearance to show unsealed state
                if (TryComp<AppearanceComponent>(partUid.Value, out var appearance))
                    _appearance.SetData(partUid.Value, ToggleableVisuals.Enabled, false, appearance);
                
                // Make part unremovable even when unsealed
                EnsureComp<UnremoveableComponent>(partUid.Value);
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
            _audio.PlayGlobal(component.DeploySound, user, AudioParams.Default.WithVolume(-5f));

        // Retract parts
        RetractPart(uid, component, component.Helmet, component.HelmetContainerId);
        RetractPart(uid, component, component.Chestplate, component.ChestplateContainerId);
        RetractPart(uid, component, component.Gauntlets, component.GauntletsContainerId);
        RetractPart(uid, component, component.Boots, component.BootsContainerId);

        component.Deployed = false;
        component.Activating = false;
        Dirty(uid, component);
    }

    private void RetractPart(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, string containerId)
    {
        if (partUid == null || component.Wearer == null)
            return;

        // Check if entity is valid
        if (!Exists(partUid.Value))
            return;

        // Update part component to maintain link
        if (TryComp<ModsuitPartComponent>(partUid.Value, out var partComp))
        {
            partComp.Control = uid; // Ensure link is maintained
            partComp.Sealed = false;
            Dirty(partUid.Value, partComp);
            
            // Update appearance to show unsealed state before unequipping
            if (TryComp<AppearanceComponent>(partUid.Value, out var appearance))
                _appearance.SetData(partUid.Value, ToggleableVisuals.Enabled, false, appearance);
        }

        // Remove unremoveable component to allow unequip
        RemComp<UnremoveableComponent>(partUid.Value);

        // Check if part is already in a container - if so, we're done
        if (_container.TryGetContainingContainer(partUid.Value, out var existingContainer))
        {
            // Already in a container, nothing to do
            return;
        }

        // Find which slot this part is in and unequip it
        bool wasUnequipped = false;
        if (_inventory.TryGetContainerSlotEnumerator(component.Wearer.Value, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (_inventory.TryGetSlotEntity(component.Wearer.Value, slot.ID, out var slotEntity) && slotEntity == partUid.Value)
                {
                    _inventory.TryUnequip(component.Wearer.Value, slot.ID, force: true, silent: true);
                    wasUnequipped = true;
                    break;
                }
            }
        }

        // Only insert into container if we actually unequipped it and it's not already in a container
        if (wasUnequipped && !_container.TryGetContainingContainer(partUid.Value, out _))
        {
            var container = _container.EnsureContainer<Container>(uid, containerId);
            _container.Insert(partUid.Value, container);
        }
    }

    public void SetActive(EntityUid uid, ModsuitControlComponent component, bool active, EntityUid? user = null)
    {
        if (component.Active == active)
            return;

        component.Active = active;

        if (active)
        {
            if (component.ActivateSound != null && user != null)
                _audio.PlayGlobal(component.ActivateSound, user.Value, AudioParams.Default.WithVolume(-5f));
            
            // Seal parts sequentially with delays
            SealPartSequential(uid, component, component.Helmet, true, 0f);
            SealPartSequential(uid, component, component.Chestplate, true, 0.5f);
            SealPartSequential(uid, component, component.Gauntlets, true, 1f);
            SealPartSequential(uid, component, component.Boots, true, 1.5f);
            
            // Make control unit unremoveable
            EnsureComp<UnremoveableComponent>(uid);
        }
        else
        {
            if (component.DeactivateSound != null && user != null)
                _audio.PlayGlobal(component.DeactivateSound, user.Value, AudioParams.Default.WithVolume(-5f));
            
            // Unseal parts sequentially when deactivating
            UnsealPartSequential(uid, component, component.Helmet, 0f);
            UnsealPartSequential(uid, component, component.Chestplate, 0.5f);
            UnsealPartSequential(uid, component, component.Gauntlets, 1f);
            UnsealPartSequential(uid, component, component.Boots, 1.5f);
            
            // Allow control unit removal after delaye
            Timer.Spawn(TimeSpan.FromSeconds(2f), () => 
            {
                if (Exists(uid))
                    RemComp<UnremoveableComponent>(uid);
            });
        }

        // Update movement speed
        if (component.Wearer != null)
            _movementSpeed.RefreshMovementSpeedModifiers(component.Wearer.Value);

        Dirty(uid, component);
    }

    private void SealPart(EntityUid? partUid, bool seal)
    {
        if (partUid == null)
            return;

        if (TryComp<ModsuitPartComponent>(partUid.Value, out var partComp))
        {
            partComp.Sealed = seal;
            
            // Update appearance to show sealed/unsealed state
            if (TryComp<AppearanceComponent>(partUid.Value, out var appearance))
                _appearance.SetData(partUid.Value, ToggleableVisuals.Enabled, seal, appearance);
            
            Dirty(partUid.Value, partComp);
        }
    }

    private void SealPartSequential(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, bool seal, float delay)
    {
        if (partUid == null)
            return;

        if (delay > 0f)
        {
            // Schedule the sealing with a delay
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (component.Active) // Only seal if still active
                {
                    SealPart(partUid, seal);
                }
            });
        }
        else
        {
            SealPart(partUid, seal);
        }
    }

    private void UnsealPartSequential(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, float delay)
    {
        if (partUid == null)
            return;

        if (delay > 0f)
        {
            // Schedule the unsealing with a delay
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!component.Active) // Only unseal if still inactive
                {
                    SealPart(partUid, false);
                }
            });
        }
        else
        {
            SealPart(partUid, false);
        }
    }

    private void UpdateActivateActionIcon(EntityUid uid, ModsuitControlComponent component, bool primed)
    {
        if (component.ActivateActionEntity == null)
            return;

        // Change icon based on primed state - activate-ready when primed, activate when not
        var icon = new SpriteSpecifier.Rsi(
            new ResPath("Interface/Actions/actions_mod.rsi"),
            primed ? "activate-ready" : "activate"
        );
        
        _actions.SetIcon(component.ActivateActionEntity.Value, icon);
    }
}

public sealed partial class ModsuitDeployActionEvent : InstantActionEvent
{
}

public sealed partial class ModsuitActivateActionEvent : InstantActionEvent
{
}
