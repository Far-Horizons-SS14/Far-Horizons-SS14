using Content.Server.Antag;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Station event component for spawning this rules antags in space around a station.
/// </summary>
public sealed partial class SpaceSpawnRule : StationEventSystem<SpaceSpawnRuleComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, SpaceSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);
        // Far-Horizons - moved the rest of this method to TrySetCoordinates. The issue is that the original code
        // tries to find a station and pick a spawn near it when the rule is added, but the rule can be added
        // before we spawn a station.
    }

    // Far Horizons - Most of the body of this is from upstream's Added function, but the calls to remove the rule
    // have been replaced with "return false", and a "return true" has been added as the default at the end.
    private bool TrySetCoordinates(EntityUid uid, SpaceSpawnRuleComponent comp)
    {
        if (!TryGetRandomStation(out var station))
            return false;

        // find a station grid
        var gridUid = StationSystem.GetLargestGrid(station.Value);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Warning("Chosen station has no grids, cannot pick location for {ToPrettyString(uid):rule}");
            return false;
        }

        // figure out its AABB size and use that as a guide to how far the spawner should be
        var size = grid.LocalAABB.Size.Length() / 2;
        var distance = size + comp.SpawnDistance;
        var angle = RobustRandom.NextAngle();
        // position relative to station center
        var location = angle.ToVec() * distance;

        // create the spawner!
        var xform = Transform(gridUid.Value);
        var position = _transform.GetWorldPosition(xform) + location;
        comp.Coords = new MapCoordinates(position, xform.MapID);
        Sawmill.Info($"Picked location {comp.Coords} for {ToPrettyString(uid):rule}");
        return true;
    }

    private void OnSelectLocation(Entity<SpaceSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords == null && !TrySetCoordinates(ent.Owner, ent.Comp))
        {
            Sawmill.Warning($"Cannot select a space spawn location for {ToPrettyString(ent.Owner):rule}");
            return;
        }

        if (ent.Comp.Coords is {} coords)
            args.Coordinates.Add(coords);
    }
}
