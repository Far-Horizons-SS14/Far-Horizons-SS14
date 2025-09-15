using Content.Shared.FarHorizons.Tools.Shipyard;
using Content.Shared.FarHorizons.Tools.Shipyard.Components;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.UI.Shipyard
{
    public sealed class ShipLabelerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        [ViewVariables]
        private ShipLabelerWindow? _window;

        public ShipLabelerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<ShipLabelerWindow>();

            if (_entManager.TryGetComponent(Owner, out ShipLabelerComponent? labeler))
            {
                _window.SetNameMaxLength(labeler!.NameMaxChars);
            }

            _window.OnRename += OnRename;

            Reload();
        }

        public void Reload()
        {
            if (_window == null || 
                !_entManager.TryGetComponent(Owner, out TransformComponent? transform) || 
                transform.GridUid == null || 
                !_entManager.TryGetComponent(transform.GridUid, out MetaDataComponent? metadata))
                return;

            _window.SetCurrentName(metadata.EntityName);
        }

        private void OnRename(string name) {
            if (_window == null ||
                name == "")
                return;
            
            _window.SetStatus(Loc.GetString("ship-labeler-status-waiting"));
            SendPredictedMessage(new ShipLabelerNameChangeRequest(name));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null ||
                message is not ShipLabelerNameChangeResponse resp)
                return;
            
            if (resp.Success)
                _window.SetStatus(Loc.GetString("ship-labeler-status-success"));
            else
                _window.SetStatus(Loc.GetString("ship-labeler-status-error", ("error", resp.Error)));
        }
    }
}
