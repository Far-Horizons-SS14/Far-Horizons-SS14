using Content.Shared.Inventory;

namespace Content.Shared.Clothing;

public sealed partial class ClothingSpeedModifierSystem
{
    public void InitializeFH()
        => SubscribeLocalEvent<ClothingSpeedModifierComponent, InventoryRelayedEvent<ClothingSpeedModifierQueryEvent>>(OnSlowdownQuery);

    private void OnSlowdownQuery(Entity<ClothingSpeedModifierComponent> ent, ref InventoryRelayedEvent<ClothingSpeedModifierQueryEvent> query)
    {
        if(ent.Comp.RequireActivated) return;

        query.Args.TotalSprintCoefficient *= ent.Comp.SprintModifier;
        query.Args.TotalWalkCoefficient *=ent.Comp.WalkModifier;
    }

    public bool TrySetSpeedModifier(EntityUid ent, EntityUid wearer, float modifier, ClothingSpeedModifierComponent? component = null) 
    => TrySetSpeedModifier(ent, wearer, modifier, modifier, component);

    public bool TrySetSpeedModifier(EntityUid ent, EntityUid wearer, float walkspeedMod, float sprintspeedMod, ClothingSpeedModifierComponent? component = null)
    {
        if(!Resolve(ent, ref component))
            return false;

        component.SprintModifier = sprintspeedMod;
        component.WalkModifier = walkspeedMod;
        Dirty<ClothingSpeedModifierComponent>((ent, component));
        _movementSpeed.RefreshMovementSpeedModifiers(wearer);
        return true;
    }
}

public sealed class ClothingSpeedModifierQueryEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    /// All slots to relay to
    /// </summary>
    public SlotFlags TargetSlots { get; }

    /// <summary>
    /// Total clothing speed modifier for Sprinting
    /// </summary>
    public float TotalSprintCoefficient = 1.0f;

    /// <summary>
    /// Total clothing speed modifier for Walking
    /// </summary>
    public float TotalWalkCoefficient = 1.0f;
    public ClothingSpeedModifierQueryEvent(SlotFlags slots) 
        => TargetSlots = slots;
}