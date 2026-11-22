using Content.Client.UserInterface.Fragments;
using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.CartridgeLoader.Cartridges;

public sealed partial class FrontierBankUi : UIFragment
{
    private FrontierBankUiFragment? _control;
    public override Control GetUIFragmentRoot() => 
        _control!;

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not FrontierBankUiState cast)
            return;
        
        _control?.Populate(cast);
    }

    public override void Setup(BoundUserInterface ui, EntityUid? owner)
    {
        _control = new FrontierBankUiFragment();
        _control.WithdrawRequest += v => WithdrawRequest(ui, v);
        _control.DepositRequest += v => DepositRequest(ui, v);
    }

    private void WithdrawRequest(BoundUserInterface ui, int val)
    {
        var withdrawRequest = new FrontierBankCredsticWithdrawMessageEvent(val);
        var message = new CartridgeUiMessage(withdrawRequest);
        ui.SendMessage(message);
    }

    private void DepositRequest(BoundUserInterface ui, int val)
    {
        var depositRequest = new FrontierBankCredsticDepositMessageEvent(val);
        var message = new CartridgeUiMessage(depositRequest);
        ui.SendMessage(message);
    }
}