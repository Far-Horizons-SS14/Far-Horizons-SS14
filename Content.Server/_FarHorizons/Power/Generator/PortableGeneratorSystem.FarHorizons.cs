using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Access.Systems;
using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

public sealed partial class PortableGeneratorSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SmartGridSystem _smartGrid = default!;

    [SubscribeLocalEvent]
    private void OnSetSmartGridAvailabilityOverride(
        EntityUid uid,
        PortableGeneratorComponent component,
        SmartGridSetAvailabilityOverrideMessage args)
    {
        if (!_accessReader.IsAllowed(args.Actor, uid))
            return;

        if (_smartGrid.SetAvailabilityOverride(uid, args.Priority))
            UpdateUI(uid, component, Comp<FuelGeneratorComponent>(uid), Comp<PowerSupplierComponent>(uid));
    }
}
