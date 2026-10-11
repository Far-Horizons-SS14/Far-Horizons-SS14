using Content.Shared._FarHorizons.UI.BackgroundTraits;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.UI.BackgroundTraits;

public sealed partial class BackgroundTraitsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BackgroundTraitsWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BackgroundTraitsWindow>();
        _window.OnStart(Owner);
        _window.SelectTraitsCallback += SubmitTraits;
    }

    private void SubmitTraits(List<ProtoId<BackgroundTraitPrototype>> traits)
        => SendMessage(new SubmitBackgroundTraitSelectionMessage(traits));
}