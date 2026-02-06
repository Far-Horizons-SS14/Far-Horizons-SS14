
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client._FarHorizons.Vehicle.Equipment;
public sealed partial class VehicleEquipmentSystems : EntitySystem
{    
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleEquipmentComponent, ComponentInit>(OnInit);
        base.Initialize();
    }

    private void OnInit(Entity<VehicleEquipmentComponent> ent, ref ComponentInit args)
    {
        if(!HasComp<SpriteComponent>(ent.Owner) || !HasComp<PointLightComponent>(ent.Owner)) return;
        
        _sprite.SetVisible(ent.Owner, false);
    }
}