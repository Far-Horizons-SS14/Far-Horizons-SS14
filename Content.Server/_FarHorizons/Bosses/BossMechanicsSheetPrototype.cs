using Content.Shared._FarHorizons.Bosses;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FarHorizons.Bosses;

[Prototype]
public sealed partial class BossMechanicsSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public List<BossMechanic> Mechanics = [];
}

[DataDefinition]
public sealed partial class BossMechanic
{
    [DataField] public List<IBossMechanicConsideration> Considerations = [];
    [DataField] public List<IBossMechanicLogic> Logic = [];
    [DataField] public int CooldownSeconds;
    [DataField] public int InitialCooldown;
}

public interface IBossMechanicLogic
{
    void Run(IEntityManager entMan, IRobustRandom random, Entity<BossCombatComponent> ent);
}