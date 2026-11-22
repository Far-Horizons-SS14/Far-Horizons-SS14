using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;

namespace Content.Server._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphFollowupAttackOnTriggerSystem : EntitySystem
{
    [Dependency] private TelegraphedAttackSystem _telegraph = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphFollowupAttackOnTriggerComponent, OnTelegraphTriggered>(OnTriggered);
    }

    private void OnTriggered(Entity<TelegraphFollowupAttackOnTriggerComponent> ent, ref OnTelegraphTriggered args)
    {
        if (!_transform.TryGetMapOrGridCoordinates(ent, out var coords) ||
            !TryComp<TelegraphedAttackComponent>(ent, out var telegraph))
            return;
        
        _telegraph.DelayedSpawnAttack(ent.Comp.Entity, coords.Value, telegraph.IgnoreTargets);
    }
}