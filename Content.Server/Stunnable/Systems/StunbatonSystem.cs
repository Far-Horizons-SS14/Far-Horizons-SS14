using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.PowerCell;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stunnable;

namespace Content.Server.Stunnable.Systems
{
    public sealed partial class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private RiggableSystem _riggableSystem = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private PowerCellSystem _powerCell = default!; // 🌟Starlight🌟
        [Dependency] private SharedBatterySystem _battery = default!;
        [Dependency] private ItemToggleSystem _itemToggle = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
        }

        private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            // 🌟Starlight🌟 start
            if (!_itemToggle.IsActivated(entity.Owner))
            {
                args.Cancelled = true;
                return;
            }

            Entity<BatteryComponent>? batteryEntity = null;
            BatteryComponent? battery = null;
            EntityUid batteryUid = entity.Owner;
            
            if (TryComp(entity.Owner, out battery))
            {
                batteryUid = entity.Owner;
            }
            else if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEntity))
            {
                batteryUid = batteryEntity.Value.Owner;
                battery = batteryEntity.Value.Comp;
            }
            
            if (battery == null || !_battery.TryUseCharge((batteryUid, battery), entity.Comp.EnergyPerUse))
            {
                args.Cancelled = true;
            }
            // 🌟Starlight🌟 end
        }

        private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(entity.Owner)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(onMsg);

            // 🌟Starlight🌟 start
            Entity<BatteryComponent>? batteryEnt = null;
            BatteryComponent? battery = null;
            EntityUid batteryUid = entity.Owner;
            
            if (TryComp<BatteryComponent>(entity.Owner, out battery))
            {
                batteryUid = entity.Owner;
            }
            else if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt))
            {
                batteryUid = batteryEnt.Value.Owner;
                battery = batteryEnt.Value.Comp;
            }
            
            if (battery != null)
            {
                var count = (int)(_battery.GetCharge((batteryUid, battery)) / entity.Comp.EnergyPerUse);
                args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
            }
            // 🌟Starlight🌟 end
        }

        protected override void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
        {
            base.TryTurnOn(entity, ref args);

            // 🌟Starlight🌟 start
            Entity<BatteryComponent>? batteryEnt = null;
            BatteryComponent? battery = null;
            EntityUid batteryUid = entity.Owner;
            
            if (TryComp<BatteryComponent>(entity.Owner, out battery))
            {
                batteryUid = entity.Owner;
            }
            else if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt))
            {
                batteryUid = batteryEnt.Value.Owner;
                battery = batteryEnt.Value.Comp;
            }
            
            if (battery != null && _battery.GetCharge((batteryUid, battery)) < entity.Comp.EnergyPerUse)
            {
                args.Cancelled = true;
                if (args.User != null)
                {
                    _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid)args.User, (EntityUid)args.User);
                }
                return;
            }
            // 🌟Starlight🌟 end

            if (TryComp<RiggableComponent>(entity, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(entity.Owner, battery != null ? _battery.GetCharge((batteryUid, battery)) : 0f, args.User);
            }
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(Entity<StunbatonComponent> entity, ref SolutionChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(entity, out var riggable) ||
                !TryComp<BatteryComponent>(entity, out var battery))
                return;

            if (_itemToggle.IsActivated(entity.Owner) && riggable.IsRigged)
                _riggableSystem.Explode(entity.Owner, _battery.GetCharge((entity, battery)));
        }

        // TODO: Not used anywhere?
        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            });
        }

        private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
        {
            // 🌟Starlight🌟 start
            // If the stun baton is not activated, we don't need to check anything
            if (!_itemToggle.IsActivated(entity.Owner))
                return;

            Entity<BatteryComponent>? batteryEnt = null;
            BatteryComponent? battery = null;
            EntityUid batteryUid = entity.Owner;
            
            if (TryComp<BatteryComponent>(entity.Owner, out battery))
            {
                batteryUid = entity.Owner;
            }
            else if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out batteryEnt))
            {
                batteryUid = batteryEnt.Value.Owner;
                battery = batteryEnt.Value.Comp;
            }
            
            // Deactivate if: no battery found OR battery charge is too low
            if (battery == null || _battery.GetCharge((batteryUid, battery)) < entity.Comp.EnergyPerUse)
            {
                _itemToggle.TryDeactivate(entity.Owner, predicted: false);
            }
            // 🌟Starlight🌟 end
        }
    }
}