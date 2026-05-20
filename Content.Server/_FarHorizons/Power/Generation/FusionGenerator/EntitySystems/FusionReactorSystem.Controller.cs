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
        if (fusionReactor.MasterController == null)
            return;

        var (uid, comp) = fusionReactor.MasterController.Value;

        var extractTarget = comp.PowerExtraction * dt;

        if (extractTarget <= 0)
            return;

        fusionReactor.Plasma.AddJoule(-extractTarget);

        var storedEnergy = AddPower(fusionReactor, extractTarget);

        if (!TryComp<PowerSupplierComponent>(uid, out var powerSupplier))
            return;

        powerSupplier.MaxSupply = Math.Max(0, extractTarget - storedEnergy);
    }
}