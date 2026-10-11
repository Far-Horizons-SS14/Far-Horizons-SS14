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
    public float Tickrate { get; private set; }
    public float TickTime { get; private set; }

    private void InitializeCVars()
    {
        Subs.CVar(_cfg, FHCCVars.FusionReactorAllowMassDestruction, value => AllowMassDestruction = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorStage3Delay, value => Stage3Delay = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorStage4Delay, value => Stage4Delay = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMin, value => ExplosiveForceMin = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMax, value => ExplosiveForceMax = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorExplosiveForceMaxDestruction, value => ExplosiveForceMaxDestruction = value, true);
        Subs.CVar(_cfg, FHCCVars.FusionReactorTargetTickrate, TickrateCalc, true);

        void TickrateCalc(float value)
        {
            /// Some explaination:
            /// A maximum of 30 as that's the base tickrate of the game and it can't go faster
            /// A minimum of 0.1 (10 seconds per tick) as the simulation gets unstable going slower
            /// The minimum also prevents div by zero and negative numbers
            value = Math.Clamp(value, 0.1f, 30f);
            Tickrate = value;
            TickTime = 1 / value;
            _cfg.SetCVar(FHCCVars.FusionReactorTargetTickrate, value);
        }
    }
}