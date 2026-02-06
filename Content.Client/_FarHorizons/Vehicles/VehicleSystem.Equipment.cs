
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared._FarHorizons.Vehicles;
using Robust.Client.GameObjects;

namespace Content.Client._FarHorizons.Vehicle.Equipment;
public sealed partial class VehicleEquipmentSystems : EntitySystem
{    
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        SubscribeNetworkEvent<InstalledVehicleEquipment>(OnEquipmentInstalled);
        base.Initialize();
    }

    private void OnEquipmentInstalled(InstalledVehicleEquipment ev)
    {
        if (!TryGetEntity(ev.Part, out var part))
            return;
        if(!HasComp<VehicleEquipmentComponent>(part)) return;
        if(!TryComp<SpriteComponent>(part, out var sprite) || !HasComp<PointLightComponent>(part)) return;
        _sprite.SetVisible((part.Value, sprite), false);
    }
}