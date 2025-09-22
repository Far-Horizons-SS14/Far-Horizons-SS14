using Content.Shared._FarHorizons.Numbness;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Client.Player;

namespace Content.Client._FarHorizons.Numbness;

public sealed class NumbnessSystem : SharedNumbnessSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NumbnessStatusEffectComponent, StatusEffectAppliedEvent>(OnNumbnessApply);
        SubscribeLocalEvent<NumbnessStatusEffectComponent, StatusEffectRemovedEvent>(OnNumbnessShutdown);
    }

    private void OnNumbnessApply(Entity<NumbnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        var component = new PainNumbnessComponent();
        component.Temporary = true;
        EntityManager.AddComponent(args.Target, component);
    }

    private void OnNumbnessShutdown(Entity<NumbnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (EntityManager.TryGetComponent<PainNumbnessComponent>(args.Target, out var component))
        {
            if (component.Temporary)
            {
                EntityManager.RemoveComponent<PainNumbnessComponent>(args.Target);
            }
        }
    }
}
