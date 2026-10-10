using Content.Shared._FarHorizons.Traits;
using Content.Shared.Buckle;

namespace Content.Server.Traits.Assorted;

public sealed partial class BuckeOnTraitsAppliedSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckleSystem = default!;

    [SubscribeLocalEvent]
    private void OnTraitsApplied(Entity<BuckeOnTraitsAppliedComponent> ent, ref TraitsApplied _)
    {
        var buckle = Spawn(ent.Comp.Prototype, Transform(ent).Coordinates);
        _buckleSystem.TryBuckle(ent, ent, buckle);
    }
}
