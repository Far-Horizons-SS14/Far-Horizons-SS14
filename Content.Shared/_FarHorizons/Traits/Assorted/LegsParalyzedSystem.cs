using Content.Shared.Buckle.Components;
using Content.Shared.Stunnable;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;

namespace Content.Shared._FarHorizons.Traits.Assorted;

public sealed partial class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _stand = default!;

    [SubscribeLocalEvent(after:[typeof(StandingStateSystem)])]
    private void OnStartup(Entity<LegsParalyzedComponent> ent,  ref ComponentStartup _)
        => AddComponents(ent);

    [SubscribeLocalEvent(after:[typeof(StandingStateSystem)])]
    private void OnShutdown(Entity<LegsParalyzedComponent> ent, ref ComponentShutdown _)
        => RemoveComponents(ent);

    [SubscribeLocalEvent(after:[typeof(StandingStateSystem)])]
    private void OnBuckled(Entity<LegsParalyzedComponent> ent, ref BuckledEvent args)
        => RemoveComponents(ent);

    [SubscribeLocalEvent(after:[typeof(StandingStateSystem)])]
    private void OnUnbuckled(Entity<LegsParalyzedComponent> ent, ref UnbuckledEvent args)
        => AddComponents(ent);

    private void RemoveComponents(EntityUid ent)
    {
        if(TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<WormComponent>(ent); 
        RemCompDeferred<KnockedDownComponent>(ent);
    }

    private void AddComponents(EntityUid ent)
    {
        if(TerminatingOrDeleted(ent))
            return;
            
        EnsureComp<WormComponent>(ent); 
        EnsureComp<KnockedDownComponent>(ent);
    }
}
