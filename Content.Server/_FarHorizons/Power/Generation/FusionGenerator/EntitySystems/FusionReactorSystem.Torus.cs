using Content.Server._FarHorizons.Fusion;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Random;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private void TorusInitialize()
    {

    }

    public void SetMagnet(EntityUid uid, FusionReactorTorusComponent? torus, bool state)
    {
        if (!Resolve(uid, ref torus))
            return;
        if (state == torus.IsMagnet)
            return;

        torus.IsMagnet = state;
    }

    public void ProcessCooling(List<Entity<FusionReactorTorusComponent>> magnets, GasMixture coolant)
    {
        var totalHeatCap = 0f;
        var totalThermalEnergy = 0f;

        totalHeatCap += _atmosphereSystem.GetHeatCapacity(coolant, true);
        totalThermalEnergy += _atmosphereSystem.GetThermalEnergy(coolant);

        foreach (var (uid, magnet) in magnets)
        {
            if (!magnet.IsMagnet)
                continue;

            totalHeatCap += magnet.ThermalMass;
            totalThermalEnergy += magnet.Temperature * magnet.ThermalMass;
        }

        var tEquilibrium = totalThermalEnergy / totalHeatCap;

        // For now assume there is perfect and instantaneous heat exchange
        foreach (var (uid, magnet) in magnets)
        {
            if (!magnet.IsMagnet)
                continue;

            magnet.Temperature = tEquilibrium;
        }

        coolant.Temperature = tEquilibrium;
    }

    public void ProcessDamage(List<Entity<FusionReactorTorusComponent>> tori, FusionMixture fusionMixture)
    {
        var pressure = fusionMixture.ConstrainedPressure;
        var temperature = fusionMixture.Temperature;

        if (pressure == 0)
            return;

        foreach (var (uid, torus) in tori)
        {
            if (torus.IsMagnet)
                continue;

            // Added a little bit of randomness so the entire thing wouldn't blow at once
            torus.Integrity -= (float)(pressure / torus.PressureResistance * (temperature / torus.TemperatureResistance)) * _random.NextFloat(0.5f, 1);

            if (torus.Integrity <= 0)
                IntegrityFailure(torus);
        }
    }

    void IntegrityFailure(FusionReactorTorusComponent torus)
    {
        /// TODO: boom
        /// boom strength should be calculated by contained fusion mixture
        torus.Temperature = float.PositiveInfinity;
        torus.IsMagnet = false;
    }
}