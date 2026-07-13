using Robust.Shared.Configuration;

namespace Content.Shared._FarHorizons.CCVar;

public sealed partial class FHCCVars
{
    /// <summary>
    /// Changes how much energy it takes to heat/cool a <see cref="FusionMixture"/>. 64x means
    /// gases heat up and cool down 64x faster than real life.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="CCVar.AtmosHeatScale"/>.
    /// </remarks>
    public static readonly CVarDef<float> FusionHeatScale =
        CVarDef.Create("fusion.heat_scale", 8f, CVar.SERVERONLY);

    /// <summary>
    /// Changes the matter consumption rate of fusion reactions. 4x means it takes 4x more matter
    /// to make the same amount of energy.
    /// </summary>
    public static readonly CVarDef<float> FusionMassScale =
        CVarDef.Create("fusion.mass_scale", 2f, CVar.SERVERONLY);

    /// <summary>
    /// Changes the amount of energy created per fusion reaction.
    /// </summary>
    public static readonly CVarDef<float> FusionEnergyScale =
        CVarDef.Create("fusion.energy_scale", 1f, CVar.SERVERONLY);
    
}
