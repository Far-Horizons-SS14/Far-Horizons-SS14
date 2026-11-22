using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class ToggleableClothingSystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    public void InitializeMultiple()
    {
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, ClothingSlotToggle>(OnToggleClothing);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, ComponentRemove>(OnRemoveToggleable);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, GotUnequippedEvent>(OnToggleableUnequip);

        SubscribeLocalEvent<ToggleableClothingMultipleComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ToggleableClothingMultipleComponent, ToggleClothingDoAfterEvent>(OnDoAfterComplete);

        SubscribeLocalEvent<ToggleableClothingMultipleComponent, OpenClothingToggleRadial>(OnOpenClothingToggleRadial);
    }

    private void OnOpenClothingToggleRadial(Entity<ToggleableClothingMultipleComponent> ent, ref OpenClothingToggleRadial args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), ClothingToggleRadialMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), ClothingToggleRadialMenuUiKey.Key, args.Performer);
    }

    private void GetRelayedVerbs(EntityUid uid, ToggleableClothingMultipleComponent component, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args) 
        => OnGetVerbs(uid, component, args.Args);

    private void OnGetVerbs(EntityUid uid, ToggleableClothingMultipleComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || component.ClothingUids.Count == 0 || component.Container == null)
            return;

        var verbCat = new VerbCategory("Toggle Equipment", "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png");

        foreach(var clothing in component.ClothingUids)
        {
            var text = component.VerbTexts.GetValueOrDefault(clothing.Key) ?? (component.ActionEntity == null ? null : Name(component.ActionEntity.Value));
            if (text == null)
                return;

            if (!_inventorySystem.InSlotWithFlags(uid, component.RequiredFlags))
                return;

            var wearer = Transform(uid).ParentUid;
            if (args.User != wearer && component.StripDelay == null)
                return;

            var verb = new EquipmentVerb()
            {
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Text = Loc.GetString(text),
                Category = verbCat
            };

            if (args.User == wearer)
            {
                verb.EventTarget = uid;
                verb.ExecutionEventArgs = new ToggleClothingEvent() { Performer = args.User };
            }
            else
            {
                verb.Act = () => StartDoAfter(args.User, uid, clothing.Key, Transform(uid).ParentUid, component);
            }

            args.Verbs.Add(verb);
        }
    }
    

    private void StartDoAfter(EntityUid user, EntityUid item, string slot, EntityUid wearer, ToggleableClothingMultipleComponent component)
    {
        if (component.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, item, component.StripDelay.Value);
        var ev = new ToggleClothingDoAfterEvent()
        {
            Slot = slot
        };

        var args = new DoAfterArgs(EntityManager, user, time, ev, item, wearer, item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", item));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnDoAfterComplete(EntityUid uid, ToggleableClothingMultipleComponent component, ToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        ToggleClothing(args.Target.Value, uid, component, args.Slot);
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that all the clothing gets removed
    /// </summary>
    private void OnToggleableUnequip(EntityUid uid, ToggleableClothingMultipleComponent component, GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var clothing in component.ClothingUids)
        {
            if (component.Container != null && clothing.Value != null && component.isActiveList.GetValueOrDefault(clothing.Key))
                ToggleClothing(args.EquipTarget, uid, component, clothing.Key, args.EquipTarget);
        }
    }

    private void OnRemoveToggleable(EntityUid uid, ToggleableClothingMultipleComponent component, ComponentRemove args)
    {
        _actionsSystem.RemoveAction(component.ActionEntity);

        foreach(var clothing in component.ClothingUids)
        {
            if (clothing.Value != null && !_netMan.IsClient)
                QueueDel(clothing.Value);
        }
    }

    /// <summary>
    ///     Equip or unequip the toggleable clothing.
    /// </summary>
    private void OnToggleClothing(EntityUid uid, ToggleableClothingMultipleComponent component, ClothingSlotToggle args) 
        => ToggleClothing(args.Actor, uid, component, args.Slot);

    private void ToggleClothing(EntityUid user, EntityUid target, ToggleableClothingMultipleComponent component, string slot, EntityUid? wearer = null)
    {
        if (component.Container == null || !component.ClothingUids.TryGetValue(slot, out var clothing) || clothing == null)
            return;

        var parent = wearer ?? Transform(target).ParentUid;
        var replace = component.ReplaceExistingClothing.GetValueOrDefault(slot);
        var isItOccuppied = _inventorySystem.TryGetSlotEntity(parent, slot, out var existing);

        var occupiedByOther = isItOccuppied && existing != null && existing.Value != clothing.Value;

        if (occupiedByOther && !replace)
        {
            _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing!.Value)), user, user);
            return;
        }

        if (component.isActiveList[slot] && _inventorySystem.TryUnequip(user, parent, slot, force: true))
        {
            component.isActiveList[slot] = false;

            if (occupiedByOther && _inventorySystem.TryEquip(user, clothing.Value, slot, force: true, triggerHandContact: true))
            {
                _containerSystem.Insert(existing!.Value, component.Container);
                component.ClothingUids[slot] = existing;
            }
            else
                _containerSystem.Insert(clothing.Value, component.Container);
        }
        else
        {
            if (occupiedByOther && _inventorySystem.TryUnequip(user, parent, slot, force: true))
            {
                _containerSystem.Insert(existing!.Value, component.Container);
                component.ClothingUids[slot] = existing;
            }

            if(_inventorySystem.TryEquip(user, clothing.Value, slot, triggerHandContact: true))
                component.isActiveList[slot] = true;
        }

        Dirty(target, component);
    }

    private void OnGetActions(EntityUid uid, ToggleableClothingMultipleComponent component, GetItemActionsEvent args)
    {
        if (component.ClothingUids.Count != 0
            && component.ActionEntity != null
            && (args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(component.ActionEntity.Value);
        }
    }

    private void OnInit(EntityUid uid, ToggleableClothingMultipleComponent component, ComponentInit args) 
        => component.Container = _containerSystem.EnsureContainer<Container>(uid, component.ContainerId);

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the toggled-entity.
    /// </summary>
    private void OnMapInit(EntityUid uid, ToggleableClothingMultipleComponent component, MapInitEvent args)
    {
        if (component.Container!.ContainedEntities is {} ents)
        {
            foreach(var ent in ents)
            {
                foreach(var clothing in component.ClothingUids)
                {
                    DebugTools.Assert(clothing.Value == ent, "Unexpected entity present inside of a toggleable clothing container.");
                    return;
                }
            }
        }

        if (component.ClothingUids.Count != 0 && component.ActionEntity != null)
        {
            foreach (var clothing in component.ClothingUids)
            {
                DebugTools.Assert(Exists(clothing.Value), "Toggleable clothing is missing expected entity.");
                DebugTools.Assert(TryComp(clothing.Value, out AttachedClothingComponent? comp), "Toggleable clothing is missing an attached component");
                DebugTools.Assert(comp?.AttachedUid == uid, "Toggleable clothing uid mismatch");
            }
        }
        else
        {
            foreach(var clothingProto in component.ClothingPrototypes)
            {
                var xform = Transform(uid);
                var clothingEnt = Spawn(clothingProto.Value, xform.Coordinates);
                var attachedClothing = EnsureComp<AttachedClothingComponent>(clothingEnt);
                attachedClothing.AttachedUid = uid;
                attachedClothing.Slot = clothingProto.Key;
                Dirty(clothingEnt, attachedClothing);
                _containerSystem.Insert(clothingEnt, component.Container, containerXform: xform);
                component.ClothingUids.Add(clothingProto.Key, clothingEnt);
                component.isActiveList.Add(clothingProto.Key, false);
                Dirty(uid, component);
            }
        }

        if (_actionContainer.EnsureAction(uid, ref component.ActionEntity, out var action, component.Action))
            _actionsSystem.SetEntityIcon((component.ActionEntity.Value, action), uid);
    }
}
