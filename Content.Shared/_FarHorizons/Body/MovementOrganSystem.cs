using System.Linq;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Traits.Assorted;

namespace Content.Shared._FarHorizons.Body;

public sealed partial class MovementOrganSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const float NoLegsModifier = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementOrganExpectedToMoveComponent, RefreshMovementSpeedModifiersEvent>(OnMovementModifierRefresh);
        SubscribeLocalEvent<MovementOrganComponent, OrganGotRemovedEvent>((_, ref args) => RefreshModifiers(args.Target));
        SubscribeLocalEvent<MovementOrganComponent, OrganGotInsertedEvent>((_, ref args) => RefreshModifiers(args.Target));
    }

    private void RefreshModifiers(EntityUid target)
    {
        if (TerminatingOrDeleted(target)) return;

        _movementSpeed.RefreshMovementSpeedModifiers(target);
    }

    private void OnMovementModifierRefresh(Entity<MovementOrganExpectedToMoveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body)) return;

        if (body.Organs == null || body.Organs.Count == 0) return;

        var allLegs = body.Organs.ContainedEntities.Select(CompOrNull<MovementOrganComponent>).Where(p => p != null)
            .ToList();

        var shoesEquipped = _inventory.TryGetSlotEntity(ent, "shoes", out _);

        var walkSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.WalkSpeedModifier);
        var sprintSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.SprintSpeedModifier);

        var totalWalkModifier = walkSpeedModifier / ent.Comp.ExpectedAmount;
        var totalSprintModifier = sprintSpeedModifier / ent.Comp.ExpectedAmount;

        if (allLegs.Count == 0)
        {
            totalWalkModifier = NoLegsModifier;
            totalSprintModifier = NoLegsModifier;
            EnsureComp<LegsParalyzedComponent>(ent);
        }
            
        else if (HasComp<LegsParalyzedComponent>(ent))
            RemComp<LegsParalyzedComponent>(ent);
        
        args.ModifySpeed(totalWalkModifier, totalSprintModifier);
    }
}