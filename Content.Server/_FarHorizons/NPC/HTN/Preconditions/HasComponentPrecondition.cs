using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server._FarHorizons.NPC.HTN.Preconditions;

public sealed partial class HasComponentPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField] public bool Invert;
    [DataField] public string Comp = "";

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (Comp == "") return false;

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entMan.ComponentFactory.TryGetRegistration(Comp, out var comp))
            return false;

        return _entMan.HasComponent(owner, comp) ^ Invert;
    }
}