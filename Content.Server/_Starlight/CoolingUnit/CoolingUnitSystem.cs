using Content.Server.Body.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Starlight.CoolingUnit;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;
using Content.Shared.Inventory; //FH
using Robust.Shared.Containers; //FH

namespace Content.Server._Starlight.CoolingUnit;

public sealed partial class CoolingUnitSystem : SharedCoolingUnitSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TemperatureSystem _tempSys = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private readonly TimeSpan _updateCooldown = TimeSpan.FromSeconds(1f);
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime > _nextUpdate)
        {
            _nextUpdate = _timing.CurTime + _updateCooldown;

            //Far Horizons Start
            var query = EntityQueryEnumerator<CoolingUnitComponent, ItemToggleComponent>();
            while (query.MoveNext(out var uid, out var coolingcomp, out var itemtoggle))
            {
                if(!itemtoggle.Activated
                || !_container.TryGetContainingContainer(uid, out var parent) || parent == null 
                || !TryComp<TemperatureComponent>(parent.Owner, out var tempcomponent) 
                || !TryComp<ThermalRegulatorComponent>(parent.Owner, out var regulatorcomp))
                    continue;

                if(coolingcomp.RequiredSlots != SlotFlags.NONE && !_inventory.InSlotWithAnyFlags(uid, coolingcomp.RequiredSlots))
                    return;

                if (tempcomponent.CurrentTemperature > (coolingcomp.DesiredTemp ?? regulatorcomp.NormalBodyTemperature))
                {
                    var coolingAmount = Math.Min(coolingcomp.MaxCooling, tempcomponent.CurrentTemperature - (coolingcomp.DesiredTemp ?? regulatorcomp.NormalBodyTemperature));
                    _tempSys.ForceChangeTemperature(parent.Owner, tempcomponent.CurrentTemperature-coolingAmount, tempcomponent);
                }
            }
            //Far Horizons End
        }
    }
}