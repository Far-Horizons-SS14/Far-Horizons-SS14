using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand is unoccupied.
/// </summary>
public sealed partial class ActiveHandFreePrecondition : HTNPrecondition
{

    [DataField] public bool Invert; // Far Horizons

    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<bool>(NPCBlackboard.ActiveHandFree, out var handFree, _entManager) && handFree ^ Invert; // Far Horizons
    }
}
