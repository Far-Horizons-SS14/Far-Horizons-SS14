using System.Linq;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Body;

public sealed partial class MovementOrganSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private InventorySystem _inventory = default!;
    private const float NoLegsModifier = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementOrganExpectedToMoveComponent, MapInitEvent>((ent, ref _) => RefreshModifiers(ent), after: [typeof(InitialBodySystem)]);
        SubscribeLocalEvent<MovementOrganExpectedToMoveComponent, RefreshMovementSpeedModifiersEvent>(OnMovementModifierRefresh);
        SubscribeLocalEvent<MovementOrganComponent, OrganGotRemovedEvent>((_, ref args) => RefreshModifiers(args.Target));
        SubscribeLocalEvent<MovementOrganComponent, OrganGotInsertedEvent>((_, ref args) => RefreshModifiers(args.Target));
    }

    private void RefreshModifiers(EntityUid target)
    {
        if (TerminatingOrDeleted(target) || !TryComp<BodyComponent>(target, out var body) || body.Organs == null || body.Organs.Count == 0)
            return;

        if (TryComp<InitialBodyComponent>(target, out var initialBody) && initialBody.Spawning)
            return;

        var legCount = body.Organs.ContainedEntities.Count(HasComp<MovementOrganComponent>);

        var hasTrait = TryComp<HumanoidCharacterProfileComponent>(target, out var hcpComp)
                    && hcpComp.Profile != null
                    && hcpComp.Profile.TraitPreferences.Contains("WheelchairBound");

        if (legCount <= 1 || hasTrait)
            EnsureComp<LegsParalyzedComponent>(target);
        else if (HasComp<LegsParalyzedComponent>(target))
            RemComp<LegsParalyzedComponent>(target);

        _movementSpeed.RefreshMovementSpeedModifiers(target);
    }

    private void OnMovementModifierRefresh(Entity<MovementOrganExpectedToMoveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        
        var totalWalkModifier = NoLegsModifier;
        var totalSprintModifier = NoLegsModifier;

        if(!HasComp<LegsParalyzedComponent>(ent))
        {
            if (!TryComp<BodyComponent>(ent, out var body)) return;

            if (body.Organs == null || body.Organs.Count == 0) return;

            var allLegs = body.Organs.ContainedEntities.Select(CompOrNull<MovementOrganComponent>).Where(p => p != null)
                .ToList();

            var shoesEquipped = _inventory.TryGetSlotEntity(ent, "shoes", out _);

            var walkSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.WalkSpeedModifier);
            var sprintSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.SprintSpeedModifier);

            totalWalkModifier = walkSpeedModifier / ent.Comp.ExpectedAmount;
            totalSprintModifier = sprintSpeedModifier / ent.Comp.ExpectedAmount;
        }
        
        args.ModifySpeed(totalWalkModifier, totalSprintModifier);
    }
}