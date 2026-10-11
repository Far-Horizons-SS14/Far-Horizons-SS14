using Content.Shared._FarHorizons.Banking.Components;
using JetBrains.Annotations;

namespace Content.Client._FarHorizons.Banking.ATM;

[UsedImplicitly]
public sealed class CredstickATMBui : BoundUserInterface
{
    [ViewVariables] private CredstickATMWindow? _window;

    public CredstickATMBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = new CredstickATMWindow();
        _window.OnClose += Close;
        _window?.OpenCentered();
        _window?.WithdrawRequest += WithdrawRequest; 
    }

    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is not CredstickATMBuiState s)
            return;
        
        _window?.Populate(s);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_window == null)
            return;

        _window.OnClose -= Close;
        _window.Orphan();
        _window = null;
    }

    private void WithdrawRequest(int amount) =>
        SendMessage(new CredstickATMRequestWithdraw(amount));
}