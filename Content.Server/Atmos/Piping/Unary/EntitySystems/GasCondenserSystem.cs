using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasCondenserSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasCondenserComponent, AtmosDeviceUpdateEvent>(OnCondenserUpdated);
    }

    private void OnCondenserUpdated(Entity<GasCondenserComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!(TryComp<ApcPowerReceiverComponent>(entity, out var receiver) && _power.IsPowered(entity, receiver))
            || !_nodeContainer.TryGetNode(entity.Owner, entity.Comp.Inlet, out PipeNode? inlet)
            || !_solution.ResolveSolution(entity.Owner, entity.Comp.SolutionId, ref entity.Comp.Solution, out var solution))
        {
            return;
        }

        if (solution.AvailableVolume == 0 || inlet.Air.TotalMoles == 0)
            return;

        var molesToConvert = NumberOfMolesToConvert(receiver, inlet.Air, args.dt);
        var removed = inlet.Air.Remove(molesToConvert);
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var moles = removed[i];
            if (moles <= 0)
                continue;

            if (_atmosphereSystem.GetGas(i).Reagent is not { } gasReagent)
            {
                inlet.Air.AdjustMoles(i, moles); // Far Horizons - if a gas has no reagent, then we can't convert it, so put it back in the mixture, rather than deleting it.
                continue;
            }

            var moleToReagentMultiplier = entity.Comp.MolesToReagentMultiplier;
            var amount = FixedPoint2.Min(FixedPoint2.New(moles * moleToReagentMultiplier), solution.AvailableVolume);
            if (amount <= 0)
            {
                inlet.Air.AdjustMoles(i, moles); // Far Horizons - if we can't add any more reagent to the solution, then put the gas back in the mixture, rather than deleting it.
                continue;
            }

            solution.AddReagent(gasReagent, amount);

            // if we have leftover reagent, then convert it back to moles and put it back in the mixture.
            inlet.Air.AdjustMoles(i, moles - (amount.Float() / moleToReagentMultiplier));
        }

        _solution.UpdateChemicals(entity.Comp.Solution.Value);
    }

    public float NumberOfMolesToConvert(ApcPowerReceiverComponent comp, GasMixture mix, float dt)
    {
        // Far Horizons start - determine the number of moles to obtain from the input mixture, but correctly
        // Note that the upstream code here considers GetHeatCapacity, rather than GetHeatCapacity / Mol, so it's wildly innacurate with large pipenets, and the outputs swing wildly there.
        if (mix.TotalMoles == 0)
            return 0;

        var heatCapacityPerMol = _atmosphereSystem.GetHeatCapacity(mix, true) / mix.TotalMoles;
        var energy = comp.Load * dt;
        var efficiency = 0.8f; // 80% efficient at using energy to drop the gas temperature to condense it.
        var temperatureDrop = Math.Max(mix.Temperature - 90, 1); // temperature drop needed to condense the gas - this is a hack, we're using the condensation temp for O2 for all gasses. Gate it to a 1K drop to avoid divide by zero errors.
        // dimensional analysis - efficiency (scalar) * energy (J) / (temperature drop (K) * heat capacity per mol (J/(mol*K))) = moles
        return efficiency * energy / (temperatureDrop * heatCapacityPerMol);
        // Far Horizons end
        /* Far Horizons - Upstream code preserved for reference
        var hc = _atmosphereSystem.GetHeatCapacity(mix, true);
        var alpha = 0.8f; // tuned to give us 1-ish u/second of reagent conversion
        // ignores the energy needed to cool down the solution to the condensation point, but that probably adds too much difficulty and so let's not simulate that
        var energy = comp.Load * dt;
        return energy / (alpha * hc);
        */// Far Horizons - End of preserved upstream block
    }
}
