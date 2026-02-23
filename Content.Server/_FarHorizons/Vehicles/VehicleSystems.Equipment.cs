using Content.Shared._FarHorizons.Vehicles;
using Content.Shared._FarHorizons.Vehicles.Components;
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
using Robust.Shared.Timing;
using Content.Shared.PowerCell.Components;
using Content.Shared.PowerCell;
using Content.Shared._FarHorizons.ReagantDraw.Components;
using Content.Shared.UserInterface;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Server._FarHorizons.Vehicle.Atmos;

namespace Content.Server._FarHorizons.Vehicle.Equipment;
public sealed partial class VehicleEquipmentSystems : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly VehicleAtmosphereSystem _vAtmos = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleModsComponent, ComponentInit>(OnCompInit);

        SubscribeLocalEvent<RiderComponent, AddRiderActions>(OnAddActions);
        SubscribeLocalEvent<RiderComponent, RemoveRiderActions>(OnRemoveActions);

        SubscribeLocalEvent<MovementSpeedModifierComponent, InstalledVehicleEquipment>(OnMovementInstalled);
        SubscribeLocalEvent<PowerCellDrawComponent, InstalledVehicleEquipment>(OnElectricEngineInstalled);
        SubscribeLocalEvent<ReagantDrawComponent, InstalledVehicleEquipment>(OnGasEngineInstalled);
        SubscribeLocalEvent<DamageableComponent, InstalledVehicleEquipment>(OnArmorInstalled);
        SubscribeLocalEvent<PointLightComponent, InstalledVehicleEquipment>(OnLightInstalled);

        SubscribeLocalEvent<VehicleModsComponent, GridUidChangedEvent>(GridUiChanged);
        SubscribeLocalEvent<VehicleModsComponent, RefreshFrictionModifiersEvent>(OnFrictionRefresh);
        SubscribeLocalEvent<VehicleModsComponent, RefreshWeightlessModifiersEvent>(OnWeightlessRefresh);

        SubscribeLocalEvent<VehicleModsComponent, TurnOffVehicleEvent>(OnVehicleShutoff);

        SubscribeLocalEvent<ItemToggleComponent, ToggleActionEvent>(OnSirenToggle);
        SubscribeLocalEvent<VehicleComponent, ToggleIntrinsicUIEvent>(OnActionToggle);
    }

    private void OnCompInit(Entity<VehicleModsComponent> ent, ref ComponentInit args)
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
                if (_proto.TryIndex<EntityPrototype>(itemProto, out var proto))
                    if (!proto.Components.ContainsKey("VehicleEquipment"))
                        continue;
                
                var item = SpawnAtPosition(itemProto, ent.Owner.ToCoordinates());
                if(!CheckandAssign(item, ent.Comp))
                    QueueDel(item);
                    
                if(HasComp<PointLightComponent>(item))
                {
                    _transform.SetParent(item, ent.Owner);
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

    private void OnAddActions(Entity<RiderComponent> ent, ref AddRiderActions args)
    {
        if(ent.Comp.Riding == null) return;
        var vehicle = ent.Comp.Riding.Value;
        if(!TryComp<VehicleModsComponent>(vehicle, out var vmComp) || vmComp.SpawnedEquipment.Count == 0) return;
        foreach(var item in vmComp.SpawnedEquipment)
        {
            if(!TryComp<VehicleEquipmentComponent>(item, out var veComp) || veComp.ActionEntity != null)
                continue;
            _actions.AddAction(ent.Owner, ref veComp.ActionEntity, veComp.ActionProto, item);
            Dirty(item, vmComp);   
        }
    }
    private void OnRemoveActions(Entity<RiderComponent> ent, ref RemoveRiderActions args)
    {
        if(ent.Comp.Riding == null) return;
        var vehicle = ent.Comp.Riding.Value;
        if(!TryComp<VehicleModsComponent>(vehicle, out var vmComp) || vmComp.SpawnedEquipment.Count == 0) return;
        foreach(var item in vmComp.SpawnedEquipment)
        {
            if(!TryComp<VehicleEquipmentComponent>(item, out var veComp) || veComp.ActionEntity == null)
                continue;
            _actions.RemoveAction(ent.Owner, veComp.ActionEntity);   
            QueueDel(veComp.ActionEntity);
            veComp.ActionEntity = null;
            Dirty(item, vmComp);
        }
    }

    private void OnVehicleShutoff(Entity<VehicleModsComponent> ent, ref TurnOffVehicleEvent args)
    {
        if(TryComp<VehicleFanModComponent>(ent.Comp.Equipment[EquipmentType.VENTFAN], out var fanComp))
            _vAtmos.SetFanState(ent, fanComp, FanState.Off);
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

    private void OnMovementInstalled(Entity<MovementSpeedModifierComponent> ent, ref InstalledVehicleEquipment args)
    {
        if(!TryComp<VehicleEquipmentComponent>(ent.Owner, out var veComp) 
            || !TryComp<MovementSpeedModifierComponent>(args.Vehicle, out var msmComp)) return;
        var Vehicle = args.Vehicle;
        Timer.Spawn(0, () =>
        {
            switch(veComp.Slot)
            {
                case EquipmentType.TIRES:
                    _movementSpeed.RefreshFrictionModifiers(Vehicle);
                    break;
                case EquipmentType.ENGINE:
                    _movementSpeed.ChangeBaseSpeed(Vehicle, ent.Comp.BaseWalkSpeed, ent.Comp.BaseSprintSpeed, msmComp.Acceleration);
                    break;
                case EquipmentType.THURSTERS:
                    _meta.AddFlag(Vehicle, MetaDataFlags.ExtraTransformEvents);
                    break;
            }
        });
    }

    private void OnElectricEngineInstalled(Entity<PowerCellDrawComponent> ent, ref InstalledVehicleEquipment args) 
        => _powerCell.SetDrawRate(args.Vehicle, ent.Comp.DrawRate);

    private void OnGasEngineInstalled(Entity<ReagantDrawComponent> ent, ref InstalledVehicleEquipment args)
    {
        if(!TryComp<ReagantDrawComponent>(args.Vehicle, out var rdComp)) return;
        rdComp.DrainRate = ent.Comp.DrainRate;
        Dirty(args.Vehicle, rdComp);
    }

    private void OnArmorInstalled(Entity<DamageableComponent> ent, ref InstalledVehicleEquipment args)
        => _damage.SetDamageModifierSetId(args.Vehicle, ent.Comp.DamageModifierSetId);

    private void OnLightInstalled(Entity<PointLightComponent> ent, ref InstalledVehicleEquipment args)
        => _appearance.SetData(ent.Owner, EquipmentVisuals.Hidden, true);

    private void GridUiChanged(Entity<VehicleModsComponent> ent, ref GridUidChangedEvent args)
        => _movementSpeed.RefreshWeightlessModifiers(ent.Owner);

    private void OnFrictionRefresh(Entity<VehicleModsComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        if(ent.Comp.Equipment[EquipmentType.TIRES] == null 
            || !TryComp<MovementSpeedModifierComponent>(ent.Comp.Equipment[EquipmentType.TIRES], out var msmComp)) return;
        args.Acceleration = msmComp.BaseAcceleration;
        args.Friction = msmComp.BaseFriction;
        args.FrictionNoInput = msmComp.BaseFriction;
    }

    private void OnWeightlessRefresh(Entity<VehicleModsComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        if(ent.Comp.Equipment[EquipmentType.THURSTERS] == null 
            || !TryComp<MovementSpeedModifierComponent>(ent.Comp.Equipment[EquipmentType.THURSTERS], out var msmComp)) return;
        var xForm = Transform(ent.Owner);
        if(xForm.GridUid == null)
        {
            args.WeightlessAcceleration = msmComp.BaseWeightlessAcceleration;
            args.WeightlessModifier = msmComp.BaseWeightlessModifier;
            args.WeightlessFriction = msmComp.BaseWeightlessFriction*5;
            args.WeightlessFrictionNoInput = msmComp.BaseWeightlessFriction*5;
        }
    }

    private void OnActionToggle(EntityUid uid, VehicleComponent component, ToggleIntrinsicUIEvent args)
    {
        if (args.Key == null)
            return;
        if(component.Rider == null)
            return;
        args.Handled = _ui.TryToggleUi(uid, args.Key, component.Rider.Value);
    }
}