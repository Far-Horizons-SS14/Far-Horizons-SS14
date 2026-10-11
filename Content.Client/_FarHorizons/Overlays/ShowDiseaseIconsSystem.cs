using System.Linq;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ShowDiseaseIconsSystem : EquipmentHudSystem<ShowDiseaseIconsComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseCarrierComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(EntityUid uid, DiseaseCarrierComponent carrier, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (carrier.ActiveDiseases.Count == 0)
            return;

        var iconId = carrier.DiseaseIcon;
        if (string.IsNullOrEmpty(iconId))
            return;

        var showDisease = carrier.ActiveDiseases.Any(x =>
        {
            var stages = _prototype.Index(x.Key.Vector).Timers;
            var maxStage = stages.Count;

            if(x.Key.Stealth.HasFlag(DiseaseStealthFlags.Hidden) && x.Value.Stage < maxStage/2)
                return false;

            return true;
        });

        if (_prototype.Resolve(iconId, out var iconPrototype) && showDisease)
            ev.StatusIcons.Add(iconPrototype);
    }
}
