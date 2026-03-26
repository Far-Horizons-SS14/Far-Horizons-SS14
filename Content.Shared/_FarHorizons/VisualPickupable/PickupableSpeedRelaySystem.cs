using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;

namespace Content.Shared._FarHorizons.VisualPickupable;

public sealed class PickupableSpeedRelaySystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PickupableSpeedRelayComponent, GotEquippedHandEvent>(OnGotPickedUp);
        SubscribeLocalEvent<PickupableSpeedRelayComponent, GotUnequippedHandEvent>(OnGotDropped);
        SubscribeLocalEvent<PickupableSpeedRelayComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRelaySpeed);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, InventoryRelayedEvent<PickupableArmorSpeedRelayEvent>>(HandleRelayedSpeed);
    }

    private void HandleRelayedSpeed(Entity<ClothingSpeedModifierComponent> ent, ref InventoryRelayedEvent<PickupableArmorSpeedRelayEvent> args)
    {
        args.Args.WalkSpeedModifier = ent.Comp.WalkModifier;
        args.Args.SprintSpeedModifier = ent.Comp.SprintModifier;
    }

    private void OnRelaySpeed(Entity<PickupableSpeedRelayComponent> ent, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        var sprintModifier = 1f;
        var walkModifier = 1f;

        var ev = new PickupableArmorSpeedRelayEvent(walkModifier, sprintModifier);
        RaiseLocalEvent(ent.Owner, ref ev);

        if (ev.SprintSpeedModifier < 1)
            sprintModifier = (ev.SprintSpeedModifier + 1) / 2;

        if (ev.WalkSpeedModifier < 1)
            walkModifier = (ev.WalkSpeedModifier + 1) / 2;
        
        args.Args.ModifySpeed(walkModifier, sprintModifier);
    }

    private void OnGotPickedUp(Entity<PickupableSpeedRelayComponent> ent, ref GotEquippedHandEvent args) =>
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    
    private void OnGotDropped(Entity<PickupableSpeedRelayComponent> ent, ref GotUnequippedHandEvent args) =>
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
}