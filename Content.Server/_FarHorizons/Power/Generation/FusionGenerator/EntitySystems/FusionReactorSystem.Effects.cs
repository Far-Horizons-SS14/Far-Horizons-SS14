using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void UpdateEffectShake(EntityUid uid, FusionReactorCameraShakeComponent shakeComponent)
    {
        if (shakeComponent.ShakeTimes == 0)
        {
            RemCompDeferred<FusionReactorCameraShakeComponent>(uid);
            return;
        }

        shakeComponent.NextShake = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(shakeComponent.Cooldown));
        shakeComponent.ShakeTimes--;

        var vector = _random.NextVector2() * shakeComponent.Intensity;
        _sharedCameraRecoil.KickCamera(uid, vector);
    }
}