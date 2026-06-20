using Content.Shared._FarHorizons.Salvage.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowOjectiveIconsSystem : EquipmentHudSystem<ShowObjectiveIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMissionObjectiveTargetComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, SalvageMissionObjectiveTargetComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if(HasComp<IdentityComponent>(uid))
        {
            var seeIdentityEvent = new SeeIdentityAttemptEvent();
            RaiseLocalEvent(uid, seeIdentityEvent);
            if(seeIdentityEvent.Cancelled)
                return;
        }

        if (_prototype.Resolve(component.TargetStatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
