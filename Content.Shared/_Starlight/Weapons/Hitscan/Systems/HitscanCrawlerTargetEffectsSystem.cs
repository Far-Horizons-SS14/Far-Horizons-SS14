using Content.Shared._FarHorizons.Vehicles.Components; // FarHorizons
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanCrawlerTargetEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanCrawlerTargetEffectsComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanCrawlerTargetEffectsComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;
        //FarHorizons-edit Start
        var target = args.Data.HitEntity.Value;
        if(TryComp<VehicleComponent>(target, out var vehicle) && HasComp<VehicleBuckleComponent>(target) && vehicle.Rider != null)
            target = vehicle.Rider.Value;

        if (TryComp<CrawlerComponent>(target, out var standing))
        {
            _stunSystem.TryAddStunDuration(target, hitscan.Comp.StunDuration);

            _stunSystem.TryKnockdown((target, standing), hitscan.Comp.KnockdownDuration, true);

            _movementMod.TryUpdateMovementSpeedModDuration(
                target, //FarHorizons-edit end
                MovementModStatusSystem.TaserSlowdown,
                hitscan.Comp.SlowDuration,
                hitscan.Comp.WalkSpeedMultiplier,
                hitscan.Comp.RunSpeedMultiplier
            );
        }
    }
}
