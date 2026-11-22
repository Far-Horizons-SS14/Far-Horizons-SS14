using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Medical.Surgery;

[UsedImplicitly]
public sealed class SurgeryComputerBoundUserInterface : BoundUserInterface
{
    private SurgeryComputerWindow? _window;

    public SurgeryComputerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<SurgeryComputerWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window != null && message is HealthAnalyzerScannedUserMessage scannerMsg)
        {
            _window.Populate(scannerMsg.State);
            _window.SetEntity(Owner);
        }
    }
}
