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

namespace Content.Server._FarHorizons.Vehicle.Equipment;
public sealed partial class VehicleEquipmentSystems : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleModsComponent, ComponentInit>(OnCompInit);

        SubscribeLocalEvent<RiderComponent, AddRiderActions>(OnAddActions);
        SubscribeLocalEvent<RiderComponent, RemoveRiderActions>(OnRemoveActions);

        SubscribeLocalEvent<MovementSpeedModifierComponent, InstalledVehicleEquipment>(OnMovementInstalled);
        SubscribeLocalEvent<PowerCellDrawComponent, InstalledVehicleEquipment>(OnElectricEngineInstalled);
        SubscribeLocalEvent<ReagantDrawComponent, InstalledVehicleEquipment>(OnGasEngineInstalled);
        SubscribeLocalEvent<ItemToggleComponent, ToggleActionEvent>(OnSirenToggle);
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
                var ev = new InstalledVehicleEquipment{Part =  GetNetEntity(item)};
                RaiseLocalEvent(item, ev);
                RaiseNetworkEvent(ev);
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
            if(slot.Key == veComp.Slot && slot.Value == null)
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
        var xForm = Transform(ent.Owner);
        if(xForm.ParentUid == xForm.GridUid) return;
        if(!TryComp<VehicleEquipmentComponent>(ent.Owner, out var veComp) 
            || !TryComp<MovementSpeedModifierComponent>(xForm.ParentUid, out var msmComp)) return;
        
        Timer.Spawn(0, () =>
        {
            if(veComp.Slot == EquipmentType.TIRES)
            {
                _movementSpeed.ChangeBaseFriction(xForm.ParentUid, ent.Comp.BaseFriction, msmComp.BaseFriction, ent.Comp.BaseAcceleration);
                _movementSpeed.ChangeBaseSpeed(xForm.ParentUid, msmComp.BaseWalkSpeed, msmComp.BaseWalkSpeed, ent.Comp.BaseAcceleration);
            }
            else if(veComp.Slot == EquipmentType.ENGINE)
            {
                _movementSpeed.ChangeBaseSpeed(xForm.ParentUid, ent.Comp.BaseWalkSpeed, ent.Comp.BaseSprintSpeed, msmComp.Acceleration);    
            }
            _movementSpeed.RefreshFrictionModifiers(xForm.ParentUid);
            _movementSpeed.RefreshMovementSpeedModifiers(xForm.ParentUid);
        });
    }

    private void OnElectricEngineInstalled(Entity<PowerCellDrawComponent> ent, ref InstalledVehicleEquipment args)
    {
        var xForm = Transform(ent.Owner);
        if(xForm.ParentUid == xForm.GridUid) return;
        _powerCell.SetDrawRate(xForm.ParentUid, ent.Comp.DrawRate);
    }

    private void OnGasEngineInstalled(Entity<ReagantDrawComponent> ent, ref InstalledVehicleEquipment args)
    {
        var xForm = Transform(ent.Owner);
        if(xForm.ParentUid == xForm.GridUid) return;
        if(!TryComp<ReagantDrawComponent>(xForm.ParentUid, out var rdComp)) return;
        rdComp.DrainRate = ent.Comp.DrainRate;
        Dirty(xForm.ParentUid, rdComp);
    }
}