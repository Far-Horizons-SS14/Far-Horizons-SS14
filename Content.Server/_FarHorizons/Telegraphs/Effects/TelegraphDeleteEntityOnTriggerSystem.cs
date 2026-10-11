using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;

namespace Content.Server._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphDeleteEntityOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphDeleteEntityOnTriggerComponent, OnTelegraphTriggered>(OnTriggered);
    }

    private void OnTriggered(Entity<TelegraphDeleteEntityOnTriggerComponent> ent, ref OnTelegraphTriggered args)
    {
        if (ent.Comp.Entity == null || 
            TerminatingOrDeleted(ent.Comp.Entity))
            return;
        Del(ent.Comp.Entity);
    }
}