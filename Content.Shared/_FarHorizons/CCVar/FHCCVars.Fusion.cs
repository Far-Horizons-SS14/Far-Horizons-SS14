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
        CVarDef.Create("fusion.heat_scale", 8f, CVar.SERVERONLY, Loc.GetString("fusion-cvar-description-heat-scale"));

    /// <summary>
    /// Changes the matter consumption rate of fusion reactions. 4x means it takes 4x more matter
    /// to make the same amount of energy.
    /// </summary>
    public static readonly CVarDef<float> FusionMassScale =
        CVarDef.Create("fusion.mass_scale", 8f, CVar.SERVERONLY, Loc.GetString("fusion-cvar-description-mass-scale"));

    /// <summary>
    /// Changes the amount of energy created per fusion reaction.
    /// </summary>
    public static readonly CVarDef<float> FusionEnergyScale =
        CVarDef.Create("fusion.energy_scale", 1f, CVar.SERVERONLY, Loc.GetString("fusion-cvar-description-energy-scale"));

    /// <summary>
    /// If the fusion reactor is allowed to generate a station-ending explosion.
    /// </summary>
    public static readonly CVarDef<bool> FusionReactorAllowMassDestruction =
        CVarDef.Create("fusion_reactor.allow_mass_destruction", true, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-mass-destruction"));

    /// <summary>
    /// Number of seconds the reactor will sit in stage 3 before exploding or continuing to stage 4.
    /// </summary>
    public static readonly CVarDef<float> FusionReactorStage3Delay =
        CVarDef.Create("fusion_reactor.stage_3_delay", 15f, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-delay-stage3"));

    /// <summary>
    /// Number of seconds the reactor will sit in stage 4 before exploding.
    /// </summary>
    public static readonly CVarDef<float> FusionReactorStage4Delay =
        CVarDef.Create("fusion_reactor.stage_4_delay", 30f, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-delay-stage4"));

    /// <summary>
    /// Minimum explosive power of a fusion reactor meltdown.
    /// </summary>
    public static readonly CVarDef<float> FusionReactorExplosiveForceMin =
        CVarDef.Create("fusion_reactor.explosion_min", 100f, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-explosion-min"));

    /// <summary>
    /// Maximum explosive power of a fusion reactor meltdown when <see cref="FusionReactorAllowMassDestruction"/> is false.
    /// </summary>
    public static readonly CVarDef<float> FusionReactorExplosiveForceMax =
        CVarDef.Create("fusion_reactor.explosion_max", 25000f, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-explosion-max"));

    /// <summary>
    /// Maximum explosive power of a fusion reactor meltdown when <see cref="FusionReactorAllowMassDestruction"/> is true.
    /// </summary>
    public static readonly CVarDef<float> FusionReactorExplosiveForceMaxDestruction =
        CVarDef.Create("fusion_reactor.explosion_max_destruction", 750000f, CVar.SERVERONLY, Loc.GetString("fusion-reactor-cvar-description-explosion-max-destruction"));

}
