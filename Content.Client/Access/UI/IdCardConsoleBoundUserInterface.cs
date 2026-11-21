using Content.Shared.Access;
using Content.Shared.Access.Components;
// FH start
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using Robust.Client.UserInterface;
using Content.Shared._FarHorizons.Factions;
// FH end

namespace Content.Client.Access.UI
{
    public sealed partial class IdCardConsoleBoundUserInterface : BoundUserInterface
    {

        private IdCardConsoleWindow? _window;

        public IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            // FH start
            _window = this.CreateWindow<IdCardConsoleWindow>();
            
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            var test = EntMan.GetComponent<IdCardConsoleComponent>(Owner).Factions;
            _window.ComputerFactions = EntMan.GetComponent<IdCardConsoleComponent>(Owner).Factions; // FH
            _window.Initialize(this);
            // FH end
            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (IdCardConsoleBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(string newFullName, string newJobTitle, List<ProtoId<AccessLevelPrototype>> newAccessList, ProtoId<FactionJobAssignmentPrototype>? newJobPrototype) //FH
        {
            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList,
                newJobPrototype));
        }
        // Starlight-edit: Start

        public void OnGroupSelected(ProtoId<AccessGroupPrototype> group)
        {
            SendMessage(new IdCardConsoleComponent.AccessGroupSelectedMessage(group)); // Starlight-edit
        }
        // Starlight-edit: End
    }
}
