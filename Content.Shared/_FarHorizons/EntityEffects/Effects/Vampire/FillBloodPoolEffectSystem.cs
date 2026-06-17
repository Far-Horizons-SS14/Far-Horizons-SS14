using Content.Shared._FarHorizons.Vampire;
using Content.Shared.EntityEffects;

namespace Content.Shared._FarHorizons.EntityEffects.Effects.Vampire;

public sealed partial class FillBloodPoolEffectSystem : EntityEffectSystem<LesserVampireComponent, FillBloodPool>
{
    [Dependency] private readonly SharedLesserVampireSystem _vampire = default!;
    
    protected override void Effect(Entity<LesserVampireComponent> ent, ref EntityEffectEvent<FillBloodPool> args)
    {
        var current = _vampire.GetBloodPool(ent);
        _vampire.SetBloodPool(ent, current + (args.Effect.Factor * args.Scale));
    }
}

public sealed partial class FillBloodPool : EntityEffectBase<FillBloodPool>
{
    [DataField] public float Factor = 1;
}