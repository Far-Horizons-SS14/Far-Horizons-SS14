using Content.Shared._FarHorizons.Banking.Components;
using JetBrains.Annotations;

namespace Content.Client._FarHorizons.Banking.Credstick;

[UsedImplicitly]
public sealed class CredstickTransferDialogBui : BoundUserInterface
{
    [ViewVariables] private CredstickTransferDialogWindow? _window;

    public CredstickTransferDialogBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<CredstickComponent>(Owner, out var credstick))
        {
            Close();
            return;
        }
        
        _window = new CredstickTransferDialogWindow();
        _window.SetLimit(credstick.Balance);
        _window.OnClose += Close;
        _window?.OpenCentered();
        _window?.TransferRequest += TransferRequest; 
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

    private void TransferRequest(int amount)
    {
        SendPredictedMessage(new CredstickTransferDialogConfirmed(amount));
        _window?.Close();
    }
}