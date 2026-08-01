using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
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

    private void ProcessCooling(FusionReactorNodeGroup fusionReactor, float dt)
    {
        var totalHeatCap = 0f;
        var totalThermalEnergy = 0f;

        /// Logic from the <see cref="HeatExchangerSystem"/> to determine how much gas to process
        var P = fusionReactor.CoolantIn.Pressure - fusionReactor.CoolantOut.Pressure;
        var dPdn = Atmospherics.R * ((fusionReactor.CoolantOut.Temperature / fusionReactor.CoolantOut.Volume) + (fusionReactor.CoolantIn.Temperature / fusionReactor.CoolantIn.Volume));
        var Pfinal = P * MathF.Exp(-1f * dPdn * dt);
        var n = (P - Pfinal) / dPdn;
        var coolant = n > 0 ? fusionReactor.CoolantIn.Remove(n) : fusionReactor.CoolantOut.Remove(-n);

        totalHeatCap += _atmosphereSystem.GetHeatCapacity(coolant, true);
        totalThermalEnergy += _atmosphereSystem.GetThermalEnergy(coolant);

        foreach (var (uid, magnet) in fusionReactor.Magnets)
        {
            if (!magnet.IsMagnet)
                continue;

            totalHeatCap += magnet.ThermalMass;
            totalThermalEnergy += magnet.Temperature * magnet.ThermalMass;
        }

        var tEquilibrium = totalThermalEnergy / totalHeatCap;

        // For now assume there is perfect and instantaneous heat exchange
        foreach (var (uid, magnet) in fusionReactor.Magnets)
        {
            if (!magnet.IsMagnet)
                continue;

            magnet.Temperature = tEquilibrium;
        }

        coolant.Temperature = tEquilibrium;

        if (n > 0)
            _atmosphereSystem.Merge(fusionReactor.CoolantOut, coolant);
        else
            _atmosphereSystem.Merge(fusionReactor.CoolantIn, coolant);
    }

    private void ProcessDamage(List<Entity<FusionReactorTorusComponent>> tori, FusionMixture fusionMixture, float dt)
    {
        var pressure = fusionMixture.ConstrainedPressure;
        var temperature = fusionMixture.Temperature;

        foreach (var (uid, torus) in tori)
        {
            if (pressure == 0)
            {
                torus.Integrity += MathF.Min(torus.Regeneration * dt, torus.MaxIntegrity - torus.Integrity);
                continue;
            }
            if (torus.IsMagnet)
                continue;

            torus.Integrity -= MathF.Min((float)(pressure / torus.PressureResistance * (temperature / torus.TemperatureResistance)) * dt, dt);

            if (torus.Integrity <= 0)
                IntegrityFailure(uid, torus, fusionMixture);
        }
    }

    private void IntegrityFailure(EntityUid uid, FusionReactorTorusComponent torus, FusionMixture fusionMixture)
    {
        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        var pressure = (float)(fusionMixture.Pressure + fusionMixture.ConstrainedPressure);
        var volume = (float)Math.Min(fusionMixture.ConstrainedVolume, fusionMixture.Volume) / fusionReactor.Torus.Count;

        /// joules of energy resultant from it no longer being under pressure
        var energy = pressure * volume / 0.67f * (1 - MathF.Pow(Atmospherics.OneAtmosphere * 1000 / pressure, 0.67f / 1.67f));
        /// joules of energy contained within the mixture
        energy += MathF.Cbrt((float)(_fusionSystem.GetHeatCapacity(fusionMixture) * fusionMixture.Temperature));

        var intensity = MathF.Max(energy, 80);
        _explosionSystem.QueueExplosion(uid, "Default", intensity, 100, 100, user: uid);
        QueueDel(uid);
    }

    private void ProcessMagnetics(FusionReactorNodeGroup reactorNodeGroup, float dt)
    {
        if (reactorNodeGroup.SuperconductingCount <= 0)
        {
            reactorNodeGroup.MagneticPressure = 1000;
            return;
        }

        // Power draw is RequestedMagneticPressure/SuperconductingCount * TorusCount/10 watts
        var targetDraw = reactorNodeGroup.TorusCount * reactorNodeGroup.RequestedMagneticPressure / (reactorNodeGroup.SuperconductingCount * 10);
        var dE = targetDraw / reactorNodeGroup.SuperconductingCount;
        var dP = reactorNodeGroup.RequestedMagneticPressure / reactorNodeGroup.SuperconductingCount;

        reactorNodeGroup.MagneticPressure = 0;
        foreach (var (uid, magnet) in reactorNodeGroup.Magnets)
        {
            SetPowerDraw(uid, magnet.IsMagnet && magnet.Superconducting, dE);
            SetOnSatisfy(uid, () =>
            {
                magnet.Temperature += GetPowerSupplied(uid) * dt * magnet.Loss / magnet.ThermalMass;
                reactorNodeGroup.MagneticPressure += dP * GetPowerSatisfaction(uid);
                reactorNodeGroup.Plasma.Pressure = reactorNodeGroup.MagneticPressure;
            });
        }
    }
}