using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.UI.BackgroundTraits;

public sealed partial class OpenBackgroundTraitsEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class SubmitBackgroundTraitSelectionMessage(List<ProtoId<BackgroundTraitPrototype>> selection) : BoundUserInterfaceMessage
{
    public List<ProtoId<BackgroundTraitPrototype>> Selection = selection;
}