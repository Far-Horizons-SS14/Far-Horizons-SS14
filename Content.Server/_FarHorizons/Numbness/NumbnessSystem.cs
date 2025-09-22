using Content.Shared._FarHorizons.Numbness;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;

namespace Content.Server._FarHorizons.Numbness;

public sealed class NumbnessSystem : SharedNumbnessSystem
{
    
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NumbnessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<NumbnessStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        
    }

    private void OnEffectApplied(Entity<NumbnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_entManager.HasComponent<PainNumbnessComponent>(args.Target)) return;
        var component = new PainNumbnessComponent();
        component.Temporary = true;
        _entManager.AddComponent(args.Target, component);
    }
    private void OnEffectRemoved(Entity<NumbnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_entManager.TryGetComponent<PainNumbnessComponent>(args.Target, out var painNumbness))
        {
            if (painNumbness.Temporary)
            {
                _entManager.RemoveComponent(args.Target, painNumbness);
            }
        }
    }

}
