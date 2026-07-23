using Content.Shared.Movement.Systems;

namespace Content.Shared._FarHorizons.Movement;

public sealed class BodySpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodySpeedModifierComponent, ComponentStartup>(OnCompStartUp);
        SubscribeLocalEvent<BodySpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnCompStartUp(Entity<BodySpeedModifierComponent> ent, ref ComponentStartup args)
        => _movement.RefreshMovementSpeedModifiers(ent.Owner);

    private void OnRefresh(Entity<BodySpeedModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
        => args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
}