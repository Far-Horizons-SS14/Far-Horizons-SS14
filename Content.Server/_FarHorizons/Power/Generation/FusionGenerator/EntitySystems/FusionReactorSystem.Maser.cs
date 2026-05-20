using Content.Server._FarHorizons.Fusion;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared.Ame.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;

    private void MaserInitialize()
    {
        SubscribeLocalEvent<FusionReactorMaserComponent, ComponentInit>(OnMaserInit);
        SubscribeLocalEvent<FusionReactorMaserComponent, ComponentRemove>(OnMaserRemove);
    }

    private void OnMaserInit(EntityUid uid, FusionReactorMaserComponent comp, ref ComponentInit args) => _slotsSystem.AddItemSlot(uid, FusionReactorMaserComponent.AMJarSlotId, comp.AMJarSlot);

    private void OnMaserRemove(EntityUid uid, FusionReactorMaserComponent comp, ref ComponentRemove args) => _slotsSystem.RemoveItemSlot(uid, comp.AMJarSlot);

    private void ProcessMaser(FusionReactorNodeGroup fusionReactor, float dt)
    {
        foreach (var (uid, comp) in fusionReactor.Masers)
        {
            fusionReactor.Plasma.AddJoule(DrainPower(fusionReactor, comp.PowerSetting * dt));

            if (!comp.InjectAntimatter)
                continue;

            var injectAM = dt * comp.InjectionRate;

            if (injectAM > comp.Antimatter)
            {
                if (TryComp<AmeFuelContainerComponent>(comp.AMJarSlot.Item, out var fuelContainer) && fuelContainer.FuelAmount > 0)
                {
                    fuelContainer.FuelAmount -= 1;
                    comp.Antimatter += 1;
                }
                else
                {
                    injectAM = comp.Antimatter;
                    comp.InjectAntimatter = false;
                }
            }

            if (injectAM <= 0 || comp.Antimatter <= 0)
                continue;

            comp.Antimatter -= injectAM;
            fusionReactor.Plasma.ChangeAtom(new(-1, 0), injectAM * 1e-7);
        }
    }
}