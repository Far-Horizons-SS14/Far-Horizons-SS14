using Content.Shared._FarHorizons.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public float HeatScale { get; private set; }
    public float MassScale { get; private set; }
    public float EnergyScale { get; private set; }

    private void InitializeCVars()
    {
        Subs.CVar(_cfg, FHCCVars.FusionHeatScale, value => HeatScale = 1 / value, true);
        Subs.CVar(_cfg, FHCCVars.FusionMassScale, value => MassScale = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionEnergyScale, value => EnergyScale = value, true);
    }
}