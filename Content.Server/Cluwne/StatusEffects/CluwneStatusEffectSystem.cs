using Content.Shared.Cluwne;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Cluwne.StatusEffects;

/// <summary>
/// Applies and removes the <see cref="CluwneComponent"/> while the status effect is active.
/// </summary>
public sealed class CluwneStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CluwneStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<CluwneStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<CluwneComponent>(args.Target);
    }

    private void OnRemoved(Entity<CluwneStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RemCompDeferred<CluwneComponent>(args.Target);
    }
}
