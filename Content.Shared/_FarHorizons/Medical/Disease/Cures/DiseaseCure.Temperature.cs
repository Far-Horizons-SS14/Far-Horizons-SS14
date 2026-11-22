using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Temperature.Components;
using Content.Shared.Atmos;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
public sealed partial class CureTemperature : CureStep
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    /// <summary>
    /// Cures the disease after the infection has lasted a configured duration.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();

        if(!_entityManager.TryGetComponent<TemperatureComponent>(uid, out var tempComp))
            return false;

        return tempComp.CurrentTemperature >= Min && tempComp.CurrentTemperature <= Max;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        yield return Loc.GetString("diagnoser-cure-temp", ("max", Max), ("maxC", Max-Atmospherics.T0C));
    }
}
