using Content.Client.UserInterface.Fragments;
using Content.Shared._FarHorizons.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.CartridgeLoader.Cartridges;

public sealed partial class GSLNowUi : UIFragment
{
    private GSLNowUiFragment? _control;
    public override Control GetUIFragmentRoot() => 
        _control!;

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GSLNowUiState cast)
            return;
        
        _control?.Populate(cast);
    }

    public override void Setup(BoundUserInterface ui, EntityUid? owner)
    {
        _control = new GSLNowUiFragment();
        _control.SubmitOrder += (p, r, s, i) => HandleOrder(ui, p, r, s, i);
    }

    private void HandleOrder(BoundUserInterface ui, ProtoId<CargoProductPrototype> product, string recipient, string station, string instructions)
    {
        var msg = new GSLNowOrderMessageEvent(product, recipient, station, instructions);
        var message = new CartridgeUiMessage(msg);
        ui.SendMessage(message);
    }
}