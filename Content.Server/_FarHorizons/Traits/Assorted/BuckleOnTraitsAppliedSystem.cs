using Content.Shared._FarHorizons.Traits;
using Content.Shared.Buckle;

namespace Content.Server._FarHorizons.Traits.Assorted;

public sealed partial class BuckleOnTraitsAppliedSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckleSystem = default!;

    [SubscribeLocalEvent]
    private void OnTraitsApplied(Entity<BuckleOnTraitsAppliedComponent> ent, ref TraitsApplied _)
    {
        var buckle = Spawn(ent.Comp.Prototype, Transform(ent).Coordinates);
        _buckleSystem.TryBuckle(ent, ent, buckle);
    }
}
