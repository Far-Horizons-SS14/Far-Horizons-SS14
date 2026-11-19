using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Surgery;

public interface ISurgeryStepConditionEvent
{
    Entity<HandsComponent> Actor { get; set; }
    EntityUid Tool { get; set; }
    bool Result { get; set; }
}

[ByRefEvent]
public record struct SurgeryStepConditionTestEvent(Entity<HandsComponent> Actor, EntityUid Tool, bool Result = false) : ISurgeryStepConditionEvent;

[Serializable, NetSerializable]
public sealed partial class SurgeryStepDoAfterEvent : SimpleDoAfterEvent
{
    public ProtoId<SurgeryStepPrototype> Step;
    public NetEntity? Limb;
    public NetEntity? ToolEnt;

    public SurgeryStepDoAfterEvent() { }

    public SurgeryStepDoAfterEvent(ProtoId<SurgeryStepPrototype> step, NetEntity? limb, NetEntity? toolEnt)
    {
        Step = step;
        Limb = limb;
        ToolEnt = toolEnt;
    }
}

[Serializable, NetSerializable]
public enum SurgeryOptionUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryOptionBuiState : BoundUserInterfaceState
{
    public List<ProtoId<SurgeryStepPrototype>> Steps { get; init; }
    public NetEntity Target { get; init; }

    public SurgeryOptionBuiState(List<ProtoId<SurgeryStepPrototype>> steps, NetEntity target)
    {
        Steps = steps;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryStepSelectedMessage(ProtoId<SurgeryStepPrototype> step, NetEntity target) : BoundUserInterfaceMessage
{
    public ProtoId<SurgeryStepPrototype> Step = step;
    public NetEntity Target = target;
}