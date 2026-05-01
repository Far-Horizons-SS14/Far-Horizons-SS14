using Content.Server._FarHorizons.Fusion.Components;
using Content.Server._FarHorizons.Fusion.Systems;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator;

public sealed class FusionReactorSystem : EntitySystem
{
    [Dependency] private readonly FusionSystem _fusionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FusionReactorTorusComponent, FusionDeviceUpdateEvent>(OnTorusUpdate);
    }

    private void OnTorusUpdate(EntityUid uid, FusionReactorTorusComponent comp, ref FusionDeviceUpdateEvent args)
    {
        comp.FusionMixture.Pressure = comp.MagneticPressure;
        _fusionSystem.React(comp.FusionMixture, args.dt);
    }
}