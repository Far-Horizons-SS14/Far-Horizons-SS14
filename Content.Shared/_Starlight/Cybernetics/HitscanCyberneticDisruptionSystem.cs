using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared._Starlight.Cybernetics.Components;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Cybernetics;

public sealed partial class HitscanCyberneticDisruptionSystem : EntitySystem
{
    [Dependency] private SharedCyberneticDisruptionSystem _disrupt = default!;
    [Dependency] private IGameTiming _gameTiming = default!; //FarHorizons

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanCyberneticDisruptionComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanCyberneticDisruptionComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(hitscan)); // FarHorizons

        if(rand.NextFloat() <= hitscan.Comp.DisableChance)
            _disrupt.TryAddCyberneticDisruptionDuration(args.Data.HitEntity.Value, hitscan.Comp.Duration);
    }
}
