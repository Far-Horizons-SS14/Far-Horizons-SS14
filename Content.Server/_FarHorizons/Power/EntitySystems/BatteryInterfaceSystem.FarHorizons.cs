using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Access.Systems;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

public sealed partial class BatteryInterfaceSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SmartGridSystem _smartGrid = default!;

    private void InitializeFH()
    {
        Subs.BuiEvents<BatteryInterfaceComponent>(
            BatteryUiKey.Key,
            subs =>
            {
                subs.Event<SmartGridSetAvailabilityOverrideMessage>(HandleSetSmartGridAvailabilityOverride);
                subs.Event<SmartGridToggleLocalAvailabilityMessage>(HandleToggleSmartGridLocalAvailability);
            });
    }

    private void HandleSetSmartGridAvailabilityOverride(
        Entity<BatteryInterfaceComponent> ent,
        ref SmartGridSetAvailabilityOverrideMessage args)
    {
        if (_accessReader.IsAllowed(args.Actor, ent))
            _smartGrid.SetAvailabilityOverride(ent, args.Priority);
    }

    private void HandleToggleSmartGridLocalAvailability(
        Entity<BatteryInterfaceComponent> ent,
        ref SmartGridToggleLocalAvailabilityMessage args)
    {
        if (_accessReader.IsAllowed(args.Actor, ent))
            _smartGrid.ToggleBatteryAvailability(ent, args.Priority);
    }
}
