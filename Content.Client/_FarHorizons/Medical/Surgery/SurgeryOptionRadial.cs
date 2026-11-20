using Content.Client.UserInterface.Controls;
using Content.Shared._FarHorizons.Medical.Surgery;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.Medical.Surgery;

[UsedImplicitly]
public sealed partial class SurgeryOptionRadial(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _protoMan = default!;

    private SimpleRadialMenu? _menu;
    private NetEntity? _target;
    
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if(_menu == null ||
           state is not SurgeryOptionBuiState surgeryState)
            return;

        Populate(surgeryState.Steps);
        _target = surgeryState.Target;
    }

    private void Populate(List<ProtoId<SurgeryStepPrototype>> steps)
    {
        if (_menu == null)
            return;

        var options = new List<RadialMenuOptionBase>(steps.Count);

        foreach (var stepId in steps)
        {
            if (!_protoMan.TryIndex(stepId, out var step))
                continue;

            options.Add(new RadialMenuActionOption<ProtoId<SurgeryStepPrototype>>(OnStepSelected, stepId)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(step.Icon),
                ToolTip = Loc.GetString(step.Name),
            });
        }

        _menu.SetButtons(options);
    }

    private void OnStepSelected(ProtoId<SurgeryStepPrototype> step)
    {
        if (_target != null)
            SendPredictedMessage(new SurgeryStepSelectedMessage(step, _target.Value));
    }
}