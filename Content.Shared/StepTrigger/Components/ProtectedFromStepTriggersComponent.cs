using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// This is used for cancelling preventable step trigger events if the user is wearing clothing in a valid slot or if the user itself has the component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(StepTriggerImmuneSystem))]
public sealed partial class ProtectedFromStepTriggersComponent : Component, IClothingSlots
{
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.FEET;

    // Far Horizons - add ability for PreventableStepTriggerComponent to be used for anything other than barefoot and glass shards
    [DataField] public string ProtectionKey = "default";
}
