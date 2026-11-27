using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Power.SmartGrid;

public sealed partial class SmartGridSystem
{
    private bool TryClassifyBoundary(EntityUid uid, out SmartGridBoundaryKind kind)
    {
        if (HasComp<PowerSupplierComponent>(uid))
        {
            kind = SmartGridBoundaryKind.Generator;
            return true;
        }

        if (HasComp<PowerNetworkBatteryComponent>(uid))
        {
            kind = SmartGridBoundaryKind.Battery;
            return true;
        }

        DebugTools.Assert(false, $"{ToPrettyString(uid)} has SmartGridControl but is not a generator or power-network battery.");
        kind = default;
        return false;
    }

    private static bool IsActiveSupplier(PowerSupplierComponent supplier)
    {
        return supplier.Enabled && supplier.MaxSupply > 0f;
    }

    private static bool IsActiveBattery(PowerNetworkBatteryComponent battery)
    {
        return battery.Enabled && battery.CanDischarge && battery.MaxSupply > 0f;
    }

    private static bool IsSupplierSaturated(PowerSupplierComponent supplier)
    {
        return supplier.CurrentSupply >= supplier.MaxSupply * SourceSaturation;
    }

    private static bool IsBatteryOutputSaturated(PowerNetworkBatteryComponent battery)
    {
        return GetDischarge(battery.NetworkBattery) >= battery.MaxSupply * SourceSaturation;
    }

    private bool HasActiveSource(IBasePowerNet network)
    {
        foreach (var supplier in GetSuppliers(network))
        {
            if (IsActiveSupplier(supplier))
                return true;
        }

        foreach (var battery in GetBatteries(network))
        {
            if (IsActiveBattery(battery))
                return true;
        }

        return false;
    }

    private bool HasBatteryOutput(IBasePowerNet network)
    {
        foreach (var battery in GetBatteries(network))
        {
            if (IsActiveBattery(battery))
                return true;
        }

        return false;
    }

    private float GetSupplyLimit(PowerNetworkBatteryComponent battery)
    {
        // APCs pass externally received power through to their output network.
        if (battery.CurrentReceiving > 0f
            && TryComp<ApcComponent>(battery.Owner, out var apc))
        {
            return apc.MaxLoad;
        }

        return battery.MaxSupply;
    }

    private static float GetDischarge(PowerState.Battery batteryPower)
    {
        return Math.Max(0f, batteryPower.CurrentSupply - batteryPower.CurrentReceiving);
    }

    /// <summary>
    ///     Returns whether a battery may support the given priority. Primary batteries follow their visible reserve
    ///     breaker; other batteries use their stored charge directly because they forward upstream availability.
    /// </summary>
    private bool CanBatterySupportAvailability(
        PowerNetworkBatteryComponent battery,
        SmartGridPriority availability)
    {
        if (!IsActiveBattery(battery))
            return false;

        if (IsPrimaryBattery(battery.Owner))
            return GetPrimaryBatteryAvailability(battery.Owner) >= availability;

        return HasReserve(battery.NetworkBattery, SmartGridBatteryReserve.GetCutoff(availability));
    }

    private SmartGridPriority GetPrimaryBatteryAvailability(EntityUid uid)
    {
        return CompOrNull<SmartGridDeviceAvailabilityComponent>(uid)?.Tracker.LocalAvailability
               ?? DefaultAvailability;
    }

    private bool TryGetPrimaryBatteryAvailability(IBasePowerNet network, out SmartGridPriority availability)
    {
        availability = SmartGridPriority.EssentialSystems;
        var found = false;
        foreach (var battery in GetBatteries(network))
        {
            if (!IsActiveBattery(battery) || !IsPrimaryBattery(battery.Owner))
                continue;

            availability = found
                ? (SmartGridPriority) Math.Max((byte) availability, (byte) GetPrimaryBatteryAvailability(battery.Owner))
                : GetPrimaryBatteryAvailability(battery.Owner);
            found = true;
        }

        return found;
    }

    /// <summary>
    ///     Returns whether a battery is not fed by an upstream battery-backed output and therefore tracks its own
    ///     reserve-based local availability.
    /// </summary>
    private bool IsPrimaryBattery(EntityUid uid)
    {
        return HasComp<PowerNetworkBatteryComponent>(uid) && !HasUpstreamBatteryOutput(uid);
    }

    private static IEnumerable<PowerSupplierComponent> GetSuppliers(IBasePowerNet network)
    {
        return network switch
        {
            PowerNet powerNet => powerNet.Suppliers,
            ApcNet apcNet => apcNet.Suppliers,
            _ => Array.Empty<PowerSupplierComponent>(),
        };
    }

    private IEnumerable<PowerNetworkBatteryComponent> GetBatteries(IBasePowerNet network)
    {
        switch (network)
        {
            case PowerNet powerNet:
                foreach (var discharger in powerNet.Dischargers)
                {
                    if (TryComp<PowerNetworkBatteryComponent>(discharger.Owner, out var battery))
                        yield return battery;
                }

                break;

            case ApcNet apcNet:
                foreach (var apc in apcNet.Apcs)
                {
                    if (TryComp<PowerNetworkBatteryComponent>(apc.Owner, out var battery))
                        yield return battery;
                }

                break;
        }
    }

    private bool TryGetInputNetwork(EntityUid uid, out IBasePowerNet input)
    {
        if (TryComp<BatteryChargerComponent>(uid, out var charger)
            && charger.Net is { } chargerNetwork)
        {
            input = chargerNetwork;
            return true;
        }

        input = default!;
        return false;
    }

    private SmartGridPriority GetPassthroughSignal(SmartGridBoundary boundary)
    {
        if (!TryComp<SmartGridControlComponent>(boundary.Owner, out var control))
            return DefaultAvailability;

        if (control.AvailabilityOverride is { } availability)
            return availability;

        return boundary.Input is { } input
            ? GetAvailability(input)
            : DefaultAvailability;
    }

    private bool TryGetOutputNetwork(EntityUid uid, out IBasePowerNet output)
    {
        if (TryComp<ApcComponent>(uid, out var apc)
            && apc.Net is { } apcNetwork)
        {
            output = apcNetwork;
            return true;
        }

        if (TryComp<BatteryDischargerComponent>(uid, out var discharger)
            && discharger.Net is { } dischargerNetwork)
        {
            output = dischargerNetwork;
            return true;
        }

        if (TryComp<PowerSupplierComponent>(uid, out var supplier)
            && supplier.Net is { } supplierNetwork)
        {
            output = supplierNetwork;
            return true;
        }

        output = default!;
        return false;
    }
}
