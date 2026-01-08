using Content.Shared._FarHorizons.Silicons.IPC;
using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.UserInterface;

namespace Content.Server._FarHorizons.Silicons.IPC;

public sealed partial class IPCSystem
{
    // CCvar.
    private int _maxNameLength;

    private void InitializeUI()
    {
        SubscribeLocalEvent<IPCLockComponent, BeforeActivatableUIOpenEvent>((ent, _, _) => UpdateUI(ent));
        SubscribeLocalEvent<IPCLockComponent, MobStateChangedEvent>((ent, _, _) => UpdateUI(ent));

        SubscribeLocalEvent<IPCLockComponent, IPCEjectBrainBuiMessage>(OnEjectBrainBuiMessage);
        SubscribeLocalEvent<IPCLockComponent, IPCEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<IPCLockComponent, IPCSetNameBuiMessage>(OnSetNameBuiMessage);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void OnEjectBrainBuiMessage(Entity<IPCLockComponent> ent, ref IPCEjectBrainBuiMessage args)
    {   
        if (ent.Comp.Lock.Locked || !ent.Comp.WiresPanel.Open)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.LockedPopupMessage), ent);
            _audio.PlayPvs(ent.Comp.LockedSound, ent);
            return;
        }

        EjectBrain(ent.Owner, args.Actor);
    }
    private void OnEjectBatteryBuiMessage(Entity<IPCLockComponent> ent, ref IPCEjectBatteryBuiMessage args)
    {
        if (ent.Comp.Lock.Locked || !ent.Comp.WiresPanel.Open)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.LockedPopupMessage), ent);
            _audio.PlayPvs(ent.Comp.LockedSound, ent);
            return;
        }

        EjectBattery(ent.Owner, args.Actor);
    }
    private void OnSetNameBuiMessage(Entity<IPCLockComponent> ent, ref IPCSetNameBuiMessage args)
    {
        if (args.Name.Length > _maxNameLength ||
            args.Name.Length == 0 ||
            string.IsNullOrWhiteSpace(args.Name) ||
            string.IsNullOrEmpty(args.Name))
            return;

        var name = args.Name.Trim();

        var metaData = MetaData(ent);

        if (metaData.EntityName.Equals(name, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):player} set IPC \"{ToPrettyString(ent)}\"'s name to: {name}");
        _metaData.SetEntityName(ent, name, metaData);
    }

    public void UpdateUI(EntityUid uid)
    {
        var chargePercent = 0f;
        var hasBattery = false;
        var mobState = MobState.Dead;
        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
        {
            hasBattery = true;
            chargePercent = battery.Value.Comp.LastCharge / battery.Value.Comp.MaxCharge;
        }

        if (TryComp<MobStateComponent>(uid, out var mobStateComp))
            mobState = mobStateComp.CurrentState;
        
        _ui.SetUiState(uid, IPCUiKey.Key,
            new IPCBuiState(chargePercent, hasBattery, mobState));
    }
} 