using Content.Shared._FarHorizons.Vehicles;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared.Actions;
using Content.Shared.Light.Components;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Content.Shared.Atmos.Components;

namespace Content.Server._FarHorizons.Vehicle.Equipment;
public sealed partial class VehicleEquipmentSystems : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleModsComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RiderComponent, AddRiderActions>(OnAddActions);
        SubscribeLocalEvent<RiderComponent, RemoveRiderActions>(OnRemoveActions);

        SubscribeLocalEvent<ItemToggleComponent, ToggleActionEvent>(OnSirenToggle);
    }

    private void OnCompInit(Entity<VehicleModsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.ModSlot = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModContainer);

        foreach(var itemProto in ent.Comp.StartingEquipment)
        {
            var item = SpawnAtPosition(itemProto, ent.Owner.ToCoordinates());
            if(HasComp<PointLightComponent>(item))
            {
                _transform.SetParent(item, ent.Owner);
            }
            else
                _container.Insert(item, ent.Comp.ModSlot);

            ent.Comp.SpawnedEquipment.Add(item);
            if(HasComp<GasTankComponent>(item))
                ent.Comp.GasTank = item;
            RaiseNetworkEvent(new InstalledVehicleEquipment{Part = GetNetEntity(item)});
        }
        Dirty(ent.Owner, ent.Comp);
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
}