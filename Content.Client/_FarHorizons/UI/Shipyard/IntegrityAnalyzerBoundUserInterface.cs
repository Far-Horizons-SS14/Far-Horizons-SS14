using Content.Shared.FarHorizons.Tools.Shipyard;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.UI.Shipyard
{
    [UsedImplicitly]
    public sealed class IntegrityAnalyzerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private IntegrityAnalyzerWindow? _window;

        public IntegrityAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<IntegrityAnalyzerWindow>();

            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            if (message is not IntegrityAnalyzerScannedTargetMessage cast)
                return;

            _window.Populate(cast);
        }
    }
}
