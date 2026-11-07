using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Popups;

namespace Content.Server.Power.EntitySystems;

public sealed partial class ApcSystem
{
    [Dependency] private SmartGridSystem _smartGrid = default!;

    [SubscribeLocalEvent]
    private void OnSetSmartGridAvailabilityOverride(
        EntityUid uid,
        ApcComponent component,
        SmartGridSetAvailabilityOverrideMessage args)
    {
        if (!_accessReader.IsAllowed(args.Actor, uid))
        {
            _popup.PopupCursor(Loc.GetString("apc-component-insufficient-access"),
                args.Actor, PopupType.Medium);
            return;
        }

        if (_smartGrid.SetAvailabilityOverride(uid, args.Priority))
            UpdateUIState(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnToggleSmartGridLocalAvailability(
        EntityUid uid,
        ApcComponent component,
        SmartGridToggleLocalAvailabilityMessage args)
    {
        if (_accessReader.IsAllowed(args.Actor, uid) && _smartGrid.ToggleBatteryAvailability(uid, args.Priority))
            UpdateUIState(uid, component);
    }
}
