// Far Horizons fork of upstream Content.Client/Power/APC/ApcBoundUserInterface.cs, which is excluded from the build.
using Content.Client._FarHorizons.Power.APC.UI;
using Content.Shared.Access.Systems;
using Content.Shared.APC;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.APC
{
    [UsedImplicitly]
    public sealed class ApcBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ApcMenu? _menu;

        public ApcBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ApcMenu>();
            _menu.SetEntity(Owner);
            _menu.OnBreaker += BreakerPressed;
            _menu.OnSmartGridAvailability += SetSmartGridAvailability;
            _menu.OnSmartGridBatteryAvailability += SetSmartGridBatteryAvailability;
            _menu.SetAccessEnabled(LocalPlayerHasAccess());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;
            _menu?.UpdateState(castState);
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }

        private void SetSmartGridAvailability(SmartGridPriority priority)
        {
            SendMessage(new SmartGridSetAvailabilityOverrideMessage(priority));
        }

        private void SetSmartGridBatteryAvailability(SmartGridPriority priority)
        {
            SendMessage(new SmartGridToggleLocalAvailabilityMessage(priority));
        }

        private bool LocalPlayerHasAccess()
        {
            return PlayerManager.LocalEntity is { } player
                && EntMan.System<AccessReaderSystem>().IsAllowed(player, Owner);
        }
    }
}
