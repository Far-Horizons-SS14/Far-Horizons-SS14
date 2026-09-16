using Content.Shared._Starlight.Implants.Components;
using Content.Shared.Implants;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;

namespace Content.Server._Starlight.Implants;

public sealed partial class UplinkImplantSystem : EntitySystem
{
    [Dependency] private UplinkSystem _uplink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(Entity<UplinkImplantComponent> implant, ref ImplantImplantedEvent args) => _uplink.TryAddEntityUplink(args.Implanted, FixedPoint2.New(0), out var _, implant, implant, true);
}
