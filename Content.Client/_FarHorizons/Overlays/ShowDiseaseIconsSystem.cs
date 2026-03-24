using System.Linq;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowDiseaseIconsSystem : EquipmentHudSystem<ShowDiseaseIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

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
            if(!_prototype.TryIndex(x.Key, out var disease))
                return false;

            var index = x.Value-1;

            if (index < 0 || index >= disease.Stages.Count)
            {
                Log.Error($"Invalid stage index {index} for {x.Key}");
                return false;
            }

            return (disease.Stages[index].Stealth & DiseaseStealthFlags.VeryHidden) == 0;
        });

        if (_prototype.Resolve(iconId, out var iconPrototype) && showDisease)
            ev.StatusIcons.Add(iconPrototype);
    }
}
