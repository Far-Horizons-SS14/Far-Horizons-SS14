using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Power.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void ControllerInitialize()
    {

    }

    private void ExtractPower(FusionReactorNodeGroup fusionReactor, float dt)
    {
        if (!fusionReactor.MasterController.HasValue)
            return;

        var (uid, comp) = fusionReactor.MasterController.Value;

        var extractTarget = comp.ExtractMode switch
        {
            Components.FusionReactorPowerExtractType.Watts => comp.WattSetting * dt,
            Components.FusionReactorPowerExtractType.Temperature =>
                (float)((_fusionSystem.GetHeatCapacity(fusionReactor.Plasma) * fusionReactor.Plasma.Temperature) -
                (_fusionSystem.GetHeatCapacity(fusionReactor.Plasma) * comp.TempSetting)),
            _ => 0,
        };

        if (extractTarget <= 0)
            return;

        _fusionSystem.AddJoule(fusionReactor.Plasma, -extractTarget);

        var storedEnergy = ChangePower(fusionReactor, extractTarget);
        // SetPowerDraw(uid, extractTarget > 0, -extractTarget / dt);

        if (!TryComp<PowerSupplierComponent>(uid, out var powerSupplier))
            return;

        powerSupplier.MaxSupply = Math.Max(0, extractTarget - storedEnergy) / dt;
    }
}