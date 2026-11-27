using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Power.SmartGrid;

public sealed partial class SmartGridSystem
{
    /// <summary>
    ///     Returns the last computed effective availability ceiling for a power network.
    /// </summary>
    public SmartGridPriority GetAvailability(IBasePowerNet network)
    {
        return _networkStates.GetValueOrDefault(network)?.EffectiveAvailability ?? DefaultAvailability;
    }

    /// <summary>
    ///     Gets the input and effective availability signals for a smart-grid output boundary, and whether its input
    ///     is fed by an upstream battery-backed output.
    /// </summary>
    public SmartGridAvailabilityState GetAvailabilityState(EntityUid uid)
    {
        var inputIsBatteryBacked = HasUpstreamBatteryOutput(uid);
        if (!TryComp<SmartGridControlComponent>(uid, out var control))
        {
            return new(
                InputAvailability: null,
                EffectiveAvailability: DefaultAvailability,
                LocalAvailability: DefaultAvailability,
                AvailabilityOverride: null,
                InputIsBatteryBacked: inputIsBatteryBacked);
        }

        var hasInput = TryGetInputNetwork(uid, out var input) && HasActiveSource(input);
        SmartGridPriority? inputAvailability = hasInput ? GetAvailability(input) : null;
        if (!TryGetOutputNetwork(uid, out var output))
        {
            return new(
                inputAvailability,
                DefaultAvailability,
                DefaultAvailability,
                control.AvailabilityOverride,
                inputIsBatteryBacked);
        }

        var state = _networkStates.GetValueOrDefault(output);
        var localAvailability = state?.LocalTracker.LocalAvailability ?? DefaultAvailability;
        if (IsPrimaryBattery(uid)
            && TryComp<SmartGridDeviceAvailabilityComponent>(uid, out var batteryAvailability))
        {
            localAvailability = batteryAvailability.Tracker.LocalAvailability;
        }

        return new(
            inputAvailability,
            state?.EffectiveAvailability ?? DefaultAvailability,
            localAvailability,
            control.AvailabilityOverride,
            inputIsBatteryBacked);
    }

    /// <summary>
    ///     Returns whether a boundary receives its input from an upstream battery-backed output.
    /// </summary>
    public bool HasUpstreamBatteryOutput(EntityUid uid)
    {
        return TryGetInputNetwork(uid, out var input) && HasBatteryOutput(input);
    }

    /// <summary>
    ///     Sets an output boundary's availability signal, or returns it to automatic propagation when repeated.
    /// </summary>
    public bool SetAvailabilityOverride(EntityUid uid, SmartGridPriority priority)
    {
        if (!TryComp<SmartGridControlComponent>(uid, out var control)
            || priority > MaximumAvailability)
        {
            return false;
        }

        control.AvailabilityOverride = control.AvailabilityOverride == priority ? null : priority;
        if (TryGetOutputNetwork(uid, out var output))
            _networkStates.GetOrNew(output).Reset(priority);

        if (IsPrimaryBattery(uid))
            EnsureComp<SmartGridDeviceAvailabilityComponent>(uid).Tracker.Reset(priority);

        _nextEvaluation = TimeSpan.Zero;
        return true;
    }

    /// <summary>
    /// Toggles a primary battery's reserve breaker for <paramref name="priority"/>, seeding the automatic local
    /// availability of the battery and its output network.
    /// </summary>
    /// <remarks>
    /// If <paramref name="priority"/> is already shed, the battery recovers <paramref name="priority"/> and
    /// everything below it; otherwise it sheds <paramref name="priority"/> and everything above it.
    /// </remarks>
    public bool ToggleBatteryAvailability(EntityUid uid, SmartGridPriority priority)
    {
        if (priority <= MinimumAvailability
            || priority > MaximumAvailability
            || !IsPrimaryBattery(uid)
            || !TryGetOutputNetwork(uid, out var output))
        {
            return false;
        }

        var state = _networkStates.GetOrNew(output);
        var batteryAvailability = EnsureComp<SmartGridDeviceAvailabilityComponent>(uid);
        var localAvailability = batteryAvailability.Tracker.LocalAvailability < priority
            ? priority
            : priority - 1;

        batteryAvailability.Tracker.Reset(localAvailability);
        state.Reset(localAvailability);
        _nextEvaluation = TimeSpan.Zero;
        return true;
    }

    /// <summary>
    ///     Returns the diagnostic state for a network currently participating in smart-grid evaluation.
    /// </summary>
    public bool TryGetStatus(IBasePowerNet network, out SmartGridNetworkStatus status)
    {
        if (!_networkStates.TryGetValue(network, out var state))
        {
            status = default;
            return false;
        }

        var localState = state.LocalTracker;
        var transition = localState.ShortageSince != null
            ? SmartGridTransition.Shedding
            : localState.RecoverySince != null
              || localState.RecoverySpoolUpUntil > _timing.CurTime
                ? SmartGridTransition.Recovering
                : SmartGridTransition.None;
        status = new(
            state.EffectiveAvailability,
            localState.LocalAvailability,
            localState.SourceConstrained,
            transition);
        return true;
    }

    /// <summary>
    ///     Returns immediate power sources for network diagnostics.
    /// </summary>
    public IEnumerable<SmartGridSourceStatus> GetImmediateSources(IBasePowerNet network)
    {
        foreach (var supplier in GetSuppliers(network))
        {
            var health = !IsActiveSupplier(supplier)
                ? SmartGridSourceHealth.Disabled
                : IsSupplierSaturated(supplier)
                    ? SmartGridSourceHealth.OutputCapped
                    : SmartGridSourceHealth.Healthy;
            yield return new(supplier.Owner, supplier.CurrentSupply, supplier.MaxSupply, null, null, health);
        }

        foreach (var battery in GetBatteries(network))
        {
            var batteryPower = battery.NetworkBattery;
            var supplyLimit = GetSupplyLimit(battery);
            var reserve = GetReserveCutoff(network);
            var discharge = GetDischarge(batteryPower);
            var health = !IsActiveBattery(battery)
                ? SmartGridSourceHealth.Disabled
                : batteryPower.Capacity > 0f
                  && batteryPower.CurrentStorage < batteryPower.Capacity * reserve
                  && discharge > 0f
                    ? SmartGridSourceHealth.LowReserve
                    : IsBatteryOutputSaturated(battery)
                        ? SmartGridSourceHealth.OutputCapped
                        : SmartGridSourceHealth.Healthy;
            yield return new(
                battery.Owner,
                batteryPower.CurrentSupply,
                supplyLimit,
                batteryPower.CurrentStorage,
                batteryPower.Capacity,
                health);
        }
    }
}
