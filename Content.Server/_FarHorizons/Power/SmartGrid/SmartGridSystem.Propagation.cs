using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Power.SmartGrid;

public sealed partial class SmartGridSystem
{
    private void PropagateAvailability(
        List<SmartGridBoundary> boundaries,
        Dictionary<IBasePowerNet, SmartGridPriority> manualOutputs)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            var signalsByOutput = new Dictionary<IBasePowerNet, SmartGridOutputSignalAggregation>();
            foreach (var boundary in boundaries)
            {
                if (manualOutputs.ContainsKey(boundary.Output))
                    continue;

                signalsByOutput.GetOrNew(boundary.Output).Add(
                    boundary.Kind,
                    GetDeviceOutputSignal(boundary));
            }

            foreach (var (output, signals) in signalsByOutput)
            {
                var outputState = _networkStates[output];
                var availability = signals.ApplyTo(outputState.LocalTracker.LocalAvailability);
                if (availability == outputState.EffectiveAvailability)
                    continue;

                outputState.EffectiveAvailability = availability;
                changed = true;
            }
        }
    }

    /// <summary>
    ///     Calculates the signal one boundary sends to its output network. Independent generators and batteries
    ///     contribute alternative source signals.
    /// </summary>
    private SmartGridPriority GetDeviceOutputSignal(SmartGridBoundary boundary)
    {
        if (boundary.Kind == SmartGridBoundaryKind.Generator)
        {
            return CompOrNull<SmartGridDeviceAvailabilityComponent>(boundary.Owner)?.Tracker.LocalAvailability
                   ?? DefaultAvailability;
        }

        if (boundary.Kind == SmartGridBoundaryKind.Battery)
        {
            if (IsPrimaryBattery(boundary.Owner))
                return GetPrimaryBatteryAvailability(boundary.Owner);

            if (CanBatteryBridgeInputConstraint(boundary))
                return DefaultAvailability;
        }

        return GetPassthroughSignal(boundary);
    }

    private bool CanBatteryBridgeInputConstraint(SmartGridBoundary boundary)
    {
        if (boundary.Input is not { } input
            || _networkStates[input].EffectiveAvailability == DefaultAvailability
            || _networkStates[boundary.Output].LocalTracker.LocalAvailability
            <= _networkStates[input].EffectiveAvailability
            || !SuppliersAreTheOnlyInputConstraint(input))
        {
            return false;
        }

        var availability = _networkStates[boundary.Output].LocalTracker.LocalAvailability;
        return TryComp<PowerNetworkBatteryComponent>(boundary.Owner, out var battery)
               && CanBatterySupportAvailability(battery, availability);
    }

    private bool SuppliersAreTheOnlyInputConstraint(IBasePowerNet network)
    {
        foreach (var supplier in GetSuppliers(network))
        {
            if (!IsActiveSupplier(supplier))
                continue;

            if (!IsSupplierSaturated(supplier))
                return false;
        }

        foreach (var battery in GetBatteries(network))
        {
            if (IsActiveBattery(battery))
                return false;
        }

        return true;
    }

    private void ApplyAvailability(List<EntityUid> loads, SmartGridPriority availability)
    {
        foreach (var uid in loads)
        {
            if (!TryComp<SmartGridLoadComponent>(uid, out var load))
                continue;

            _smartGridLoad.SetShed(uid, load.Priority > availability, load);
        }
    }
}
