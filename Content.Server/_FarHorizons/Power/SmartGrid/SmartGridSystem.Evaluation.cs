using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Power.SmartGrid;

public sealed partial class SmartGridSystem
{
    /// <summary>
    ///     Takes one evaluation-time view of all loads and output boundaries. Both APC and direct loads are grouped
    ///     by the network they consume from so they receive the same availability signal.
    /// </summary>
    private SmartGridEvaluationSnapshot BuildEvaluationSnapshot()
    {
        var snapshot = new SmartGridEvaluationSnapshot();

        var apcLoadQuery = EntityQueryEnumerator<SmartGridLoadComponent, ApcPowerReceiverComponent>();
        while (apcLoadQuery.MoveNext(out var uid, out _, out var receiver))
        {
            if (receiver.Provider?.Net is { } network)
                snapshot.AddLoad(network, uid);
        }

        var directLoadQuery = EntityQueryEnumerator<SmartGridLoadComponent, PowerConsumerComponent>();
        while (directLoadQuery.MoveNext(out var uid, out _, out var consumer))
        {
            if (consumer.Net is { } network)
                snapshot.AddLoad(network, uid);
        }

        var controlQuery = EntityQueryEnumerator<SmartGridControlComponent>();
        while (controlQuery.MoveNext(out var uid, out var control))
        {
            if (!TryGetOutputNetwork(uid, out var output))
                continue;

            snapshot.Networks.Add(output);
            if (control.AvailabilityOverride is { } availability)
                snapshot.AddManualOutput(output, availability);

            if (!TryClassifyBoundary(uid, out var kind))
                continue;

            if (kind == SmartGridBoundaryKind.Generator)
                snapshot.Generators.Add(uid);

            IBasePowerNet? input = null;
            if (kind != SmartGridBoundaryKind.Generator
                && TryGetInputNetwork(uid, out var candidate)
                && HasActiveSource(candidate)
                && !ReferenceEquals(candidate, output))
            {
                input = candidate;
                snapshot.Networks.Add(candidate);
            }

            snapshot.Boundaries.Add(new(uid, input, output, kind));
        }

        foreach (var network in snapshot.Networks)
        {
            foreach (var battery in GetBatteries(network))
            {
                if (IsPrimaryBattery(battery.Owner))
                    snapshot.PrimaryBatteries.TryAdd(battery.Owner, network);
            }
        }

        return snapshot;
    }

    private void RemoveInactiveStates(SmartGridEvaluationSnapshot evaluation)
    {
        RemoveInactive(_networkStates, evaluation.Networks);

        var activeDevices = new HashSet<EntityUid>(evaluation.Generators);
        activeDevices.UnionWith(evaluation.PrimaryBatteries.Keys);
        var staleDevices = new List<EntityUid>();
        var trackerQuery = EntityQueryEnumerator<SmartGridDeviceAvailabilityComponent>();
        while (trackerQuery.MoveNext(out var uid, out _))
        {
            if (!activeDevices.Contains(uid))
                staleDevices.Add(uid);
        }

        foreach (var uid in staleDevices)
        {
            RemComp<SmartGridDeviceAvailabilityComponent>(uid);
        }
    }

    private void UpdateAutomaticAvailability(SmartGridEvaluationSnapshot evaluation)
    {
        foreach (var uid in evaluation.Generators)
        {
            if (TryComp<PowerSupplierComponent>(uid, out var supplier))
                UpdateGeneratorAvailability((uid, supplier), EnsureComp<SmartGridDeviceAvailabilityComponent>(uid));
        }

        foreach (var (uid, _) in evaluation.PrimaryBatteries)
        {
            if (TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
                UpdateBatteryAvailability(battery, EnsureComp<SmartGridDeviceAvailabilityComponent>(uid).Tracker);
        }

        foreach (var network in evaluation.Networks)
        {
            var state = _networkStates.GetOrNew(network);
            UpdateLocalAvailability(network, state.LocalTracker);
            state.EffectiveAvailability = state.LocalTracker.LocalAvailability;
        }
    }

    private void ApplyManualOverrides(SmartGridEvaluationSnapshot evaluation)
    {
        foreach (var (network, availability) in evaluation.ManualOutputs)
        {
            _networkStates[network].EffectiveAvailability = availability;
        }
    }

    private void ApplyLoadAvailability(Dictionary<IBasePowerNet, List<EntityUid>> loadsByNetwork)
    {
        foreach (var (network, loads) in loadsByNetwork)
        {
            ApplyAvailability(loads, GetAvailability(network));
        }
    }

    /// <summary>
    ///     Advances a network's automatic local availability from the state of its immediate sources.
    /// </summary>
    private void UpdateLocalAvailability(IBasePowerNet network, SmartGridAvailabilityTracker state)
    {
        if (TryGetPrimaryBatteryAvailability(network, out var primaryAvailability))
        {
            state.Reset(primaryAvailability);
            return;
        }

        var constrained = SourcesConstrained(network, state.LocalAvailability);
        var recoveryReady = !constrained && HasRecoveryReserve(network, state.LocalAvailability);
        state.Update(
            _timing.CurTime,
            constrained,
            recoveryReady,
            spoolUpAfterRecovery: true,
            ShortageDuration,
            RecoveryDuration,
            RecoverySpoolUpDuration);
    }

    /// <summary>
    ///     Advances a primary battery's local reserve availability. Reserve policy controls which priority it may
    ///     advertise; the power solver separately enforces the battery's instantaneous output limit.
    /// </summary>
    private void UpdateBatteryAvailability(
        PowerNetworkBatteryComponent battery,
        SmartGridAvailabilityTracker state)
    {
        if (!IsActiveBattery(battery))
            return;

        var batteryPower = battery.NetworkBattery;
        var hasReserve = HasReserve(batteryPower, SmartGridBatteryReserve.GetCutoff(state.LocalAvailability));
        var constrained = batteryPower.CurrentReceiving <= batteryPower.CurrentSupply && !hasReserve;
        var recoveryReady = !constrained && HasBatteryRecoveryReserve(batteryPower, state.LocalAvailability);
        state.Update(
            _timing.CurTime,
            constrained,
            recoveryReady,
            spoolUpAfterRecovery: true,
            ShortageDuration,
            RecoveryDuration,
            RecoverySpoolUpDuration);
    }

    /// <summary>
    ///     Returns whether the immediate power sources to a network would constrain it.
    /// </summary>
    /// <remarks>
    ///     A single available source is enough to leave the network unconstrained. Networks without active sources
    ///     remain at General and emit no availability signal.
    /// </remarks>
    private bool SourcesConstrained(IBasePowerNet network, SmartGridPriority availability)
    {
        var hasSource = false;

        foreach (var supplier in GetSuppliers(network))
        {
            if (!IsActiveSupplier(supplier))
                continue;

            hasSource = true;
            if (!IsSupplierSaturated(supplier))
                return false;
        }

        foreach (var battery in GetBatteries(network))
        {
            if (!IsActiveBattery(battery))
                continue;

            hasSource = true;
            if (CanBatterySupportAvailability(battery, availability))
                return false;
        }

        return hasSource;
    }

    /// <summary>
    ///     Advances a generator's output signal from whether its output is saturated.
    /// </summary>
    private void UpdateGeneratorAvailability(
        Entity<PowerSupplierComponent> supplier,
        SmartGridDeviceAvailabilityComponent availability)
    {
        var state = availability.Tracker;
        if (!IsActiveSupplier(supplier.Comp))
        {
            state.Reset(DefaultAvailability);
            return;
        }

        // Generators have no reserve to check, so any unsaturated output is ready to recover.
        // Unlike networks and batteries, they have no recovery spool-up hold.
        state.Update(
            _timing.CurTime,
            IsSupplierSaturated(supplier.Comp),
            recoveryReady: true,
            spoolUpAfterRecovery: false,
            ShortageDuration,
            RecoveryDuration,
            RecoverySpoolUpDuration);
    }

    private float GetReserveCutoff(IBasePowerNet network)
    {
        var availability = _networkStates.GetValueOrDefault(network)?.LocalTracker.LocalAvailability
                           ?? DefaultAvailability;
        return SmartGridBatteryReserve.GetCutoff(availability);
    }

    /// <summary>
    ///     Returns whether any battery on a network permits recovery to the next priority. Primary batteries use their
    ///     tracked reserve-breaker state; other batteries use their stored reserve directly.
    /// </summary>
    private bool HasRecoveryReserve(IBasePowerNet network, SmartGridPriority availability)
    {
        if (availability == DefaultAvailability)
            return true;

        var hasBattery = false;
        foreach (var battery in GetBatteries(network))
        {
            if (!IsActiveBattery(battery))
                continue;

            hasBattery = true;
            if (CanBatterySupportAvailability(battery, availability + 1))
                return true;
        }

        return !hasBattery;
    }

    /// <summary>
    ///     Returns whether a battery has enough charge to restore the priority after <paramref name="availability"/>.
    /// </summary>
    private static bool HasBatteryRecoveryReserve(PowerState.Battery batteryPower, SmartGridPriority availability)
    {
        return availability == DefaultAvailability
               || HasReserve(batteryPower, SmartGridBatteryReserve.GetRecovery(availability + 1));
    }

    /// <summary>
    ///     Returns whether a battery holds at least <paramref name="fraction"/> of its capacity.
    ///     Batteries without capacity always qualify.
    /// </summary>
    private static bool HasReserve(PowerState.Battery batteryPower, float fraction)
    {
        return batteryPower.Capacity <= 0f
               || batteryPower.CurrentStorage >= batteryPower.Capacity * fraction;
    }

    /// <summary>
    ///     Drops tracked state for keys that did not participate in the current evaluation.
    /// </summary>
    private static void RemoveInactive<TKey, TValue>(Dictionary<TKey, TValue> states, IEnumerable<TKey> active)
        where TKey : notnull
    {
        var activeSet = active as ISet<TKey> ?? new HashSet<TKey>(active);
        foreach (var key in new List<TKey>(states.Keys))
        {
            if (!activeSet.Contains(key))
                states.Remove(key);
        }
    }
}
