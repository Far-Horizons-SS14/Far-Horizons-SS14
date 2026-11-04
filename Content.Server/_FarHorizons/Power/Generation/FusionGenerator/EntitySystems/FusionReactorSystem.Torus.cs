using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
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
        UpdateMagnetAudio(uid, torus);
        UpdateTorusVisuals(uid, torus);
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

    private void ProcessMagnetics(FusionReactorNodeGroup reactorNodeGroup, float dt)
    {
        reactorNodeGroup.Magnets.ForEach(m => UpdateMagnetAudio(m.Owner, m.Comp));

        var superconducting = reactorNodeGroup.SuperconductingCount;
        if (superconducting <= 0)
        {
            reactorNodeGroup.MagneticPressure = 1000;
            foreach (var (uid, _) in reactorNodeGroup.Magnets)
                SetPowerDraw(uid, false);

            return;
        }

        var totalRequestedDraw = reactorNodeGroup.MagnetPowerRatio * reactorNodeGroup.Torus.Count * reactorNodeGroup.RequestedMagneticPressure / superconducting;
        var drawPerMagnet = totalRequestedDraw / superconducting;
        var pressurePerWatt = reactorNodeGroup.RequestedMagneticPressure / totalRequestedDraw;

        reactorNodeGroup.MagneticPressure = 0;
        foreach (var (uid, magnet) in reactorNodeGroup.Magnets)
        {
            SetPowerDraw(uid, magnet.IsMagnet && magnet.Superconducting, drawPerMagnet);
            SetOnSatisfy(uid, () =>
            {
                var suppliedJoule = GetPowerSupplied(uid) * dt;
                magnet.Temperature += suppliedJoule * magnet.Loss / magnet.ThermalMass;
                magnet.Charge += suppliedJoule;
                var usedCharge = magnet.Charge * magnet.ChargeUse * dt;
                magnet.Charge -= usedCharge;
                reactorNodeGroup.MagneticPressure += pressurePerWatt * usedCharge / dt;
                reactorNodeGroup.Plasma.Pressure = reactorNodeGroup.MagneticPressure;
            });
        }
    }

    private void UpdateMagnetAudio(EntityUid uid, FusionReactorTorusComponent comp) =>
        _ambientSoundSystem.SetAmbience(uid, comp.IsMagnet && comp.Superconducting);

    private void UpdateTorusVisuals(EntityUid uid, FusionReactorTorusComponent comp)
    {
        _appearance.SetData(uid, FusionReactorTorusVisuals.Magnet, comp.IsMagnet);
        _appearance.SetData(uid, FusionReactorTorusVisuals.Superconducting, comp.IsMagnet && comp.Superconducting);
    }
}