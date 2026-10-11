using Content.Shared._FarHorizons.Vehicles;
using Content.Shared.Actions;
using Content.Shared.Light.Components;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared._FarHorizons.ReagentDraw;
using Content.Shared.UserInterface;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Destructible;
using Robust.Shared.Random;
using Content.Server.Destructible;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Audio;

namespace Content.Server._FarHorizons.Vehicles;
public sealed partial class VehicleSystem
{
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IConfigurationManager _configManager = default!;

    private float _frictionModifier;
    private float _airfrictionModifier;
    public void InitializeEquipment()
    {
        SubscribeLocalEvent<VehicleModsComponent, ComponentStartup>(OnCompStartup);

        SubscribeLocalEvent<VehicleModsComponent, InteractUsingEvent>(OnInstallAttempt);
        SubscribeLocalEvent<VehicleModsComponent, InstallDoAfter>(OnInstallDoAfter);
        SubscribeLocalEvent<VehicleEquipmentComponent, InstalledVehicleEquipment>(OnVehicleEquipmentInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<MovementSpeedModifierComponent, InstalledVehicleEquipment>(OnMovementInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<PowerCellDrawComponent, InstalledVehicleEquipment>(OnElectricEngineInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<ReagentDrawComponent, InstalledVehicleEquipment>(OnGasEngineInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<DamageableComponent, InstalledVehicleEquipment>(OnArmorInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<PointLightComponent, InstalledVehicleEquipment>(OnLightInstalled, after:[typeof(MovementSpeedModifierSystem)]);

        SubscribeLocalEvent<VehicleModsComponent, UninstallPartMessage>(OnUninstallPart);
        SubscribeLocalEvent<VehicleModsComponent, UninstallDoAfter>(OnUninstallDoAfter);
        SubscribeLocalEvent<VehicleEquipmentComponent, UnInstalledVehicleEquipment>(OnVehicleEquipmentUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<MovementSpeedModifierComponent, UnInstalledVehicleEquipment>(OnMovementUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<PowerCellDrawComponent, UnInstalledVehicleEquipment>(OnElectricEngineUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<ReagentDrawComponent, UnInstalledVehicleEquipment>(OnGasEngineUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<DamageableComponent, UnInstalledVehicleEquipment>(OnArmorUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<PointLightComponent, UnInstalledVehicleEquipment>(OnLightUnInstalled, after:[typeof(MovementSpeedModifierSystem)]);

        SubscribeLocalEvent<RiderComponent, AddRiderActions>(OnAddActions);
        SubscribeLocalEvent<RiderComponent, RemoveRiderActions>(OnRemoveActions);
        SubscribeLocalEvent<ItemToggleComponent, ToggleActionEvent>(OnSirenToggle);
        SubscribeLocalEvent<VehicleComponent, ToggleIntrinsicUIEvent>(OnActionToggle);
        SubscribeLocalEvent<VehicleEquipmentComponent, GetItemActionsEvent>(OnGetActions);

        SubscribeLocalEvent<VehicleModsComponent, GridUidChangedEvent>(GridUiChanged);
        SubscribeLocalEvent<VehicleModsComponent, RefreshWeightlessModifiersEvent>(OnWeightlessRefresh);
        SubscribeLocalEvent<VehicleModsComponent, TurnOffVehicleEvent>(OnVehicleShutoff);
        SubscribeLocalEvent<VehicleModsComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<VehicleEquipmentComponent, BreakageEventArgs>(OnBreakageEvent);

        Subs.CVar(_configManager, CCVars.TileFrictionModifier, value => _frictionModifier = value, true);
        Subs.CVar(_configManager, CCVars.AirFriction, value => _airfrictionModifier = value, true);
    }

    private void OnCompStartup(Entity<VehicleModsComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ModSlot = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModContainer);

        var slots = ent.Comp.EquipmentSlots.GetFlags().ToArray();
        foreach(var slot in slots)
        {
            ent.Comp.Equipment.Add(slot, null);
        }
    
        if(ent.Comp.StartingEquipment.Count > 0)
        {
            foreach(var itemProto in ent.Comp.StartingEquipment)
            {
                if (_prototypes.TryIndex<EntityPrototype>(itemProto, out var proto))
                    if (!proto.Components.ContainsKey("VehicleEquipment"))
                        continue;
                
                var item = SpawnAtPosition(itemProto, ent.Owner.ToCoordinates());
                if(!CheckandAssign(item, ent.Comp))
                {
                    QueueDel(item);
                    continue;
                }
                    
                if(HasComp<PointLightComponent>(item))
                {
                    _transform.SetParent(item, ent.Owner);
                    _transform.SetLocalRotation(item, Angle.Zero);
                }
                else
                    _container.Insert(item, ent.Comp.ModSlot);
                ent.Comp.SpawnedEquipment.Add(item);
                var ev = new InstalledVehicleEquipment{Vehicle =  ent.Owner};
                RaiseLocalEvent(item, ev);
            }
        }
        Dirty(ent.Owner, ent.Comp);
    }

    #region Install Section
    private void OnInstallAttempt(Entity<VehicleModsComponent> ent, ref InteractUsingEvent args)
    {
        if(!args.Handled)
        {
            if(TryComp<VehicleEquipmentComponent>(args.Used, out var veComp) 
                && TryComp<VehicleComponent>(ent.Owner, out var vehicle))
            {
                if(vehicle.Started)
                {
                    _popupSystem.PopupCursor("Turn off vehicle before performing any maintenance.", args.User, PopupType.SmallCaution);
                    return;
                }
                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/drill_use.ogg"), ent.Owner, null);
                var installEV = new InstallDoAfter(GetNetEntity(args.Used));
                var installDoAfter = new DoAfterArgs(EntityManager, args.User, veComp.InstallandRemoveTime, installEV, ent.Owner)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    BreakOnWeightlessMove = true
                };
                _doAfter.TryStartDoAfter(installDoAfter);
                args.Handled = true;   
                }
        }
    }

    private void OnInstallDoAfter(Entity<VehicleModsComponent> ent, ref InstallDoAfter args)
    {
        if(args.Cancelled) return;
        var part = GetEntity(args.Part);
        if (part is { } uid)
        {
            if(CheckandAssign(uid, ent.Comp))
                if(HasComp<PointLightComponent>(part))
                {
                    _transform.SetParent(part, ent.Owner);
                    _transform.SetCoordinates(part, ent.Owner.ToCoordinates());
                    _transform.SetLocalRotation(part, Angle.Zero);
                }
                else
                    _container.Insert(part, ent.Comp.ModSlot);

            var ev = new InstalledVehicleEquipment{Vehicle =  ent.Owner};
            RaiseLocalEvent(uid, ev);
            Dirty(ent.Owner, ent.Comp);
        }
    }

    private void OnVehicleEquipmentInstalled(Entity<VehicleEquipmentComponent> ent, ref InstalledVehicleEquipment args)
    {
            _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionProto);
            Dirty(ent.Owner, ent.Comp);
            
            if(ent.Comp.ActionEntity == null)
                return;
            if(TryComp<DestructibleComponent>(ent.Owner, out var destructibleComp) && destructibleComp.IsBroken)
                return;
            var xForm = Transform(ent.Owner);
            if(!TryComp<VehicleComponent>(xForm.ParentUid, out var vehicle) || vehicle.Rider == null)
                return;
            _actions.GrantContainedAction(vehicle.Rider.Value, ent.Owner, ent.Comp.ActionEntity.Value);
    }

    private void OnMovementInstalled(Entity<MovementSpeedModifierComponent> ent, ref InstalledVehicleEquipment args)
    {
        if(!TryComp<VehicleEquipmentComponent>(ent.Owner, out var veComp) 
            || !TryComp<MovementSpeedModifierComponent>(args.Vehicle, out var msmComp)) return;
        var Vehicle = args.Vehicle;
        switch(veComp.Slot)
        {
            case EquipmentType.TIRES:
                _movementSpeed.ChangeBaseFriction(Vehicle, ent.Comp.BaseFriction/_frictionModifier, ent.Comp.BaseFriction/_frictionModifier, ent.Comp.BaseAcceleration);
                break;
            case EquipmentType.ENGINE:
                _movementSpeed.ChangeBaseSpeed(Vehicle, ent.Comp.BaseWalkSpeed, ent.Comp.BaseSprintSpeed, msmComp.Acceleration);
                break;
            case EquipmentType.THURSTERS:
                _meta.AddFlag(Vehicle, MetaDataFlags.ExtraTransformEvents);
                break;
            }
            _movementSpeed.RefreshFrictionModifiers(Vehicle);
            _movementSpeed.RefreshMovementSpeedModifiers(Vehicle);
    }

    private void OnElectricEngineInstalled(Entity<PowerCellDrawComponent> ent, ref InstalledVehicleEquipment args) 
        => _powerCell.SetDrawRate(args.Vehicle, ent.Comp.DrawRate);

    private void OnGasEngineInstalled(Entity<ReagentDrawComponent> ent, ref InstalledVehicleEquipment args)
    {
        if(!TryComp<ReagentDrawComponent>(args.Vehicle, out var rdComp)) return;
        rdComp.DrainRate = ent.Comp.DrainRate;
        Dirty(args.Vehicle, rdComp);
    }

    private void OnArmorInstalled(Entity<DamageableComponent> ent, ref InstalledVehicleEquipment args)
        => _damage.SetDamageModifierSetId(args.Vehicle, ent.Comp.DamageModifierSetId);

    private void OnLightInstalled(Entity<PointLightComponent> ent, ref InstalledVehicleEquipment args)
    {
        var xForm = Transform(ent.Owner);
        if(HasComp<VehicleModsComponent>(xForm.ParentUid))
            _appearance.SetData(ent.Owner, EquipmentVisuals.Hidden, true);
    }

    #endregion

    #region Uninstall Section
    private void OnUninstallPart(Entity<VehicleModsComponent> ent, ref UninstallPartMessage args)
    {
        var part = GetEntity(args.Part);
        if(!TryComp<VehicleEquipmentComponent>(part, out var veComp) || !TryComp<VehicleComponent>(ent.Owner, out var vehicle))
            return;
        
        if(vehicle.Started)
        {
            _popupSystem.PopupCursor("Turn off vehicle before performing any maintenance.", args.Actor, PopupType.SmallCaution);
            return;
        }

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/drill_use.ogg"), ent.Owner, null);
        var uninstallEV = new UninstallDoAfter(args.Part, args.Slot);
        var uninstallDoAfter = new DoAfterArgs(EntityManager, args.Actor, veComp.InstallandRemoveTime, uninstallEV, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true
        };
        _doAfter.TryStartDoAfter(uninstallDoAfter);
    }

    private void OnUninstallDoAfter(Entity<VehicleModsComponent> ent, ref UninstallDoAfter args)
    {
        if(args.Cancelled) return;
        var part = GetEntity(args.Part);
        if (part is { } uid)
        {
            _container.Remove(uid, ent.Comp.ModSlot);
            _hands.TryForcePickupAnyHand(args.User, uid);
            ent.Comp.Equipment[args.Slot] = null;
            Dirty(ent.Owner, ent.Comp);

            var ev = new UnInstalledVehicleEquipment{Vehicle =  ent.Owner};
            RaiseLocalEvent(part, ev);
        }
    }

    private void OnVehicleEquipmentUnInstalled(Entity<VehicleEquipmentComponent> ent, ref UnInstalledVehicleEquipment args)
    {
        if(ent.Comp.ActionEntity == null)
            return;

        var xForm = Transform(ent.Owner);
        if(TryComp<VehicleComponent>(xForm.ParentUid, out var vehicle) && vehicle.Rider != null)
            _actions.GrantContainedAction(vehicle.Rider.Value, ent.Owner, ent.Comp.ActionEntity.Value);

        _actions.RemoveAction(ent.Comp.ActionEntity.Value);
        ent.Comp.ActionEntity = null;
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnMovementUnInstalled(Entity<MovementSpeedModifierComponent> ent, ref UnInstalledVehicleEquipment args)
    {
        if(!TryComp<VehicleEquipmentComponent>(ent.Owner, out var veComp) 
            || !TryComp<MovementSpeedModifierComponent>(args.Vehicle, out var msmComp)) return;
        var Vehicle = args.Vehicle;
        switch(veComp.Slot)
        {
            case EquipmentType.TIRES:
                _movementSpeed.ChangeBaseFriction(Vehicle, 20/_frictionModifier, 20/_frictionModifier, 0.5f);
                break;
            case EquipmentType.ENGINE:
                _movementSpeed.ChangeBaseSpeed(Vehicle, 0, 0, msmComp.Acceleration);
                break;
            case EquipmentType.THURSTERS:
                _meta.RemoveFlag(Vehicle, MetaDataFlags.ExtraTransformEvents);
                break;
        }
        _movementSpeed.RefreshFrictionModifiers(Vehicle);
        _movementSpeed.RefreshMovementSpeedModifiers(Vehicle);
    }

    private void OnElectricEngineUnInstalled(Entity<PowerCellDrawComponent> ent, ref UnInstalledVehicleEquipment args) 
        => _powerCell.SetDrawRate(args.Vehicle, 999999);

    private void OnGasEngineUnInstalled(Entity<ReagentDrawComponent> ent, ref UnInstalledVehicleEquipment args)
    {
        if(!TryComp<ReagentDrawComponent>(args.Vehicle, out var rdComp)) return;
        rdComp.DrainRate = 999999;
        Dirty(args.Vehicle, rdComp);
    }

    private void OnArmorUnInstalled(Entity<DamageableComponent> ent, ref UnInstalledVehicleEquipment args)
        => _damage.SetDamageModifierSetId(args.Vehicle, null);

    private void OnLightUnInstalled(Entity<PointLightComponent> ent, ref UnInstalledVehicleEquipment args)
    {
        var xForm = Transform(ent.Owner);
        if(!HasComp<VehicleModsComponent>(xForm.ParentUid))
            _appearance.SetData(ent.Owner, EquipmentVisuals.Hidden, false);
    }

    #endregion

    #region Actions
    private void OnAddActions(Entity<RiderComponent> ent, ref AddRiderActions args)
    {
        if(ent.Comp.Riding == null) return;
        var vehicle = ent.Comp.Riding.Value;
        if(!TryComp<VehicleModsComponent>(vehicle, out var vmComp) || vmComp.Equipment.Count == 0) return;
        foreach(var item in vmComp.Equipment)
        {
            if(!TryComp<VehicleEquipmentComponent>(item.Value, out var veComp) || veComp.ActionEntity == null)
                continue;
            if(TryComp<DestructibleComponent>(item.Value, out var destructibleComp) && destructibleComp.IsBroken)
                continue;
            _actions.GrantContainedAction(ent.Owner, item.Value.Value, veComp.ActionEntity.Value);
        }
    }
    private void OnRemoveActions(Entity<RiderComponent> ent, ref RemoveRiderActions args)
    {
        if(ent.Comp.Riding == null) return;
        var vehicle = ent.Comp.Riding.Value;
        if(!TryComp<VehicleModsComponent>(vehicle, out var vmComp) || vmComp.Equipment.Count == 0) return;
        foreach(var item in vmComp.Equipment)
        {
            if(!TryComp<VehicleEquipmentComponent>(item.Value, out var veComp) || veComp.ActionEntity == null)
                continue;
            _actions.RemoveProvidedAction(ent.Owner, item.Value.Value, veComp.ActionEntity.Value);
        }
    }

    private void OnSirenToggle(Entity<ItemToggleComponent> ent, ref ToggleActionEvent args)
    {
        if(args.Handled) return;
        if(!TryComp<UnpoweredFlashlightComponent>(ent.Owner, out var flashComp) || !HasComp<ItemToggleComponent>(ent.Owner)) return;
        flashComp.LightOn = !flashComp.LightOn; 
        var toggleUsed = new ItemToggledEvent(false, Activated: flashComp.LightOn, args.Performer);
        RaiseLocalEvent(ent.Owner, ref toggleUsed);
        args.Handled = true;
    }

    private void OnActionToggle(EntityUid uid, VehicleComponent component, ToggleIntrinsicUIEvent args)
    {
        if (args.Key == null)
            return;
        if(component.Rider == null)
            return;
        args.Handled = _ui.TryToggleUi(uid, args.Key, component.Rider.Value);
    }

    private void OnGetActions(Entity<VehicleEquipmentComponent> ent, ref GetItemActionsEvent args)
    {
        var xForm = Transform(ent.Owner);
        if(!HasComp<VehicleModsComponent>(xForm.ParentUid))
            args.Cancel();
    }
    #endregion

    #region Functions

    private bool CheckandAssign(EntityUid item, VehicleModsComponent vmComp, VehicleEquipmentComponent? veComp=null)
    {
        if(!Resolve(item, ref veComp))
            return false;

        if((veComp.AllowedVehicles & vmComp.VehicleType) == 0)
            return false;

        foreach(var slot in vmComp.Equipment)
        {
            if((slot.Key & veComp.Slot) != 0 && slot.Value == null)
            {
                vmComp.Equipment[slot.Key] = item;
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Events

    private void OnWeightlessRefresh(Entity<VehicleModsComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        if(!ent.Comp.Equipment.TryGetValue(EquipmentType.THURSTERS, out var thrusters) || thrusters == null)
            return;
        if(!TryComp<MovementSpeedModifierComponent>(thrusters, out var msmComp)) return;
        
        var xForm = Transform(ent.Owner);
        if(xForm.GridUid == null)
        {
            args.WeightlessAcceleration = msmComp.BaseWeightlessAcceleration;
            args.WeightlessModifier = msmComp.BaseWeightlessModifier;
            args.WeightlessFriction = msmComp.BaseWeightlessFriction/_airfrictionModifier;
            args.WeightlessFrictionNoInput = msmComp.BaseWeightlessFriction/_airfrictionModifier;
        }
    }

    private void OnVehicleShutoff(Entity<VehicleModsComponent> ent, ref TurnOffVehicleEvent args)
    {
        if(!ent.Comp.Equipment.TryGetValue(EquipmentType.VENTFAN, out var fan) || fan == null)
            return;

        if(TryComp<VehicleFanModComponent>(fan, out var fanComp))
            SetFanState(ent, fanComp, FanState.Off);
    }

    private void GridUiChanged(Entity<VehicleModsComponent> ent, ref GridUidChangedEvent args)
        => _movementSpeed.RefreshWeightlessModifiers(ent.Owner);

    private void OnDamageChanged(Entity<VehicleModsComponent> ent, ref DamageChangedEvent args)
    {
        if(!args.DamageIncreased || args.DamageDelta == null) return;
        if(ent.Comp.Equipment.Count > 0)
        {
            foreach (var (slot, item) in ent.Comp.Equipment)
            {
                if(item == null || !TryComp<VehicleEquipmentComponent>(item, out var veComp))
                    continue;
                if(_random.Prob(veComp.damageChance))
                    continue;

                _damage.TryChangeDamage(item.Value, args.DamageDelta*veComp.damageTransfer, true);
            }
        }
    }

    private void OnBreakageEvent(Entity<VehicleEquipmentComponent> ent, ref BreakageEventArgs args)
    {
        var xForm = Transform(ent.Owner);
        if(xForm.GridUid == xForm.ParentUid)
            return;

        if(ent.Comp.ActionEntity != null && TryComp<VehicleComponent>(ent.Owner, out var vehicleComp))
        {
            if(vehicleComp.Rider != null)
                _actions.RemoveAction(vehicleComp.Rider.Value, ent.Comp.ActionEntity);   
            QueueDel(ent.Comp.ActionEntity);
            ent.Comp.ActionEntity = null;
            Dirty(ent.Owner, ent.Comp);
        }

        if(TryComp<DestructibleComponent>(ent.Owner, out var destructible))
            destructible.IsBroken = true;

        var ev = new UnInstalledVehicleEquipment{Vehicle =  xForm.ParentUid};
        RaiseLocalEvent(ent.Owner, ev);
    }

    #endregion
}