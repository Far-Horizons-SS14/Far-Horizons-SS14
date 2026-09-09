using Content.Shared._FarHorizons.CCVar;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    public bool AllowMassDestruction { get; private set; }
    public float Stage3Delay { get; private set; }
    public float Stage4Delay { get; private set; }
    public float ExplosiveForceMin { get; private set; }
    public float ExplosiveForceMax { get; private set; }
    public float ExplosiveForceMaxDestruction { get; private set; }

    private void InitializeCVars()
    {
        Subs.CVar(_cfg, FHCCVars.FusionReactorAllowMassDestruction, value => AllowMassDestruction = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorStage3Delay, value => Stage3Delay = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorStage4Delay, value => Stage4Delay = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMin, value => ExplosiveForceMin = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMax, value => ExplosiveForceMax = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMaxDestruction, value => ExplosiveForceMaxDestruction = value, true);
    }
}