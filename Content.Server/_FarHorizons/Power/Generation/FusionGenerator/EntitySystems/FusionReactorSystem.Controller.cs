using System.Linq;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.Atmos;
using Content.Shared.Radio;
using Robust.Shared.Audio;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void ControllerInitialize()
    {
        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerSetInjectMessage>(OnControllerSetInjectMessage);
        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerSetExtractMessage>(OnControllerSetExtractMessage);
        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerSetPressureMessage>(OnControllerSetPressureMessage);

        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerSetMaserPowerMessage>(OnControllerSetMaserPowerMessage);
        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerSetMaserInjectMessage>(OnControllerSetMaserInjectMessage);

        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerEditInjectMessage>(OnControllerEditInjectMessage);

        SubscribeLocalEvent<FusionReactorControllerComponent, FusionReactorControllerEjectMessage>(OnControllerEjectMessage);

        SubscribeLocalEvent<FusionReactorControllerComponent, BoundUIOpenedEvent>(OnControllerUIOpened);
    }

    private void ExtractPower(FusionReactorNodeGroup fusionReactor, float dt)
    {
        if (!fusionReactor.MasterController.HasValue)
            return;

        var (uid, comp) = fusionReactor.MasterController.Value;

        fusionReactor.RequestedMagneticPressure = comp.RequestedMagneticPressure;

        /// The thought here is that: 
        /// - the Watt setting, which requires constant monitoring, is always going to work, even if at reduced capacity
        /// - the Temperature setting, which is fully autonomous, is heavily dependent on stability

        var extractTarget = comp.ExtractMode switch
        {
            FusionReactorPowerExtractType.Watts => comp.WattSetting * dt * MathF.Max(fusionReactor.PlasmaStability, 0.001f),
            FusionReactorPowerExtractType.Temperature =>
                (float)(_fusionSystem.GetHeatCapacity(fusionReactor.Plasma) * (fusionReactor.Plasma.Temperature - comp.TempSetting)
                 * fusionReactor.PlasmaStability * fusionReactor.PlasmaStability), // intentional double multiply of PlasmaStability
            _ => 0,
        };

        extractTarget = MathF.Max(extractTarget, 0);

        var extracted = -_fusionSystem.ChangeJoule(fusionReactor.Plasma, -extractTarget);

        SetPowerSupply(uid, extracted > 0, (float)extracted / dt);

        SetOnSurplus(uid, () =>
        {
            if (!TryComp<PowerSupplierComponent>(uid, out var powerSupplier))
                return;

            powerSupplier.MaxSupply = GetPowerSurplus(uid);
        });

        UpdateControllerUI(uid, comp);
    }

    private void ProcessInjects(FusionReactorNodeGroup fusionReactor, float dt)
    {
        if (!fusionReactor.MasterController.HasValue)
            return;

        var (uid, comp) = fusionReactor.MasterController.Value;

        /// Can't use raw <see cref="FusionReactorNodeGroup.PlasmaStability"/> or you would never be able to fill it from empty
        var injectEfficiency = MathF.Max(fusionReactor.PlasmaStability, 0.001f);
        FusionMixture toPlasma = new() { Temperature = fusionReactor.Stored.Temperature };
        FusionMixture toStorage = new() { Temperature = fusionReactor.Plasma.Temperature };
        foreach (var (atom, data) in comp.Transfers)
        {
            switch (data.transferType)
            {
                case FusionReactorTransferType.SetRate:
                    if (data.Quantity > 0)
                    {
                        if (!fusionReactor.Stored.Atoms.TryGetValue(atom, out var ratePosStored))
                            break;
                        var amount = Math.Min(ratePosStored, data.Quantity * dt) * injectEfficiency;
                        fusionReactor.Stored.ChangeAtom(atom, -amount);
                        toPlasma.ChangeAtom(atom, amount);
                    }

                    if (data.Quantity < 0)
                    {
                        if (!fusionReactor.Plasma.Atoms.TryGetValue(atom, out var rateNegStored))
                            break;
                        var amount = Math.Min(rateNegStored, MathF.Abs(data.Quantity) * dt) * injectEfficiency;
                        fusionReactor.Plasma.ChangeAtom(atom, -amount);
                        toStorage.ChangeAtom(atom, amount);
                    }
                    break;

                case FusionReactorTransferType.SetLevel:
                    var level = fusionReactor.Plasma.Atoms.GetValueOrDefault(atom);

                    if (level < data.Quantity)
                    {
                        if (!fusionReactor.Stored.Atoms.TryGetValue(atom, out var setLvlStored))
                            break;
                        var amount = Math.Min(setLvlStored, data.Quantity - level) * injectEfficiency;
                        fusionReactor.Stored.ChangeAtom(atom, -amount);
                        toPlasma.ChangeAtom(atom, amount);
                    }

                    if (level > data.Quantity)
                    {
                        var amount = (level - data.Quantity) * injectEfficiency;
                        fusionReactor.Plasma.ChangeAtom(atom, -amount);
                        toStorage.ChangeAtom(atom, amount);
                    }
                    break;

                case FusionReactorTransferType.Fill:
                    if (!fusionReactor.Stored.Atoms.TryGetValue(atom, out var fillStored))
                        break;
                    var fillAmount = fillStored * injectEfficiency;
                    fusionReactor.Stored.ChangeAtom(atom, -fillAmount);
                    toPlasma.ChangeAtom(atom, fillAmount);
                    break;

                case FusionReactorTransferType.Drain:
                    if (!fusionReactor.Plasma.Atoms.TryGetValue(atom, out var drainStored))
                        break;
                    var drainAmount = drainStored * injectEfficiency;
                    fusionReactor.Plasma.ChangeAtom(atom, -drainAmount);
                    toStorage.ChangeAtom(atom, drainAmount);
                    break;

                default:
                    break;
            }
        }

        _fusionSystem.Merge(fusionReactor.Plasma, toPlasma);
        _fusionSystem.Merge(fusionReactor.Stored, toStorage);
    }

    #region Radio
    private void UpdateRadio(FusionReactorNodeGroup fusionReactor)
    {
        switch (fusionReactor.MeltdownStage)
        {
            case FusionReactorMeltdownStage.Stage0:
                if (fusionReactor.LastAnnouncedIntegrity != 1)
                {
                    SendRadioMessage(fusionReactor, Loc.GetString("fusion-reactor-controller-radio-integrity-restored"));
                    fusionReactor.LastAnnouncedIntegrity = 1;
                }

                if (!fusionReactor.MeltdownAnnouncements.HasFlag(FusionReactorMeltdownStage.SafeStages))
                    MeltdownAverted();
                break;

            case FusionReactorMeltdownStage.Stage1:
                IntegrityUpdate();

                if (!fusionReactor.MeltdownAnnouncements.HasFlag(FusionReactorMeltdownStage.SafeStages))
                    MeltdownAverted();
                break;

            case FusionReactorMeltdownStage.Stage2:
                IntegrityUpdate();

                if (!fusionReactor.MeltdownAnnouncements.HasFlag(FusionReactorMeltdownStage.Stage2))
                {
                    SendAnnouncement(fusionReactor, Loc.GetString("fusion-reactor-controller-announcement-stage-2"));
                    SendRadioMessage(fusionReactor, Loc.GetString("fusion-reactor-controller-radio-stage-2"));
                    fusionReactor.MeltdownAnnouncements &= ~FusionReactorMeltdownStage.SafeStages;
                    fusionReactor.MeltdownAnnouncements |= FusionReactorMeltdownStage.Stage2;
                }
                break;

            case FusionReactorMeltdownStage.Stage3:
                if (!fusionReactor.MeltdownAnnouncements.HasFlag(FusionReactorMeltdownStage.Stage3))
                {
                    SendAnnouncement(fusionReactor, Loc.GetString("fusion-reactor-controller-announcement-stage-3", ("value", Stage3Delay)));
                    fusionReactor.MeltdownAnnouncements &= ~FusionReactorMeltdownStage.SafeStages;
                    fusionReactor.MeltdownAnnouncements |= FusionReactorMeltdownStage.Stage3;
                }
                break;

            case FusionReactorMeltdownStage.Stage4:
                if (!fusionReactor.MeltdownAnnouncements.HasFlag(FusionReactorMeltdownStage.Stage4))
                {
                    SendAnnouncement(fusionReactor, Loc.GetString("fusion-reactor-controller-announcement-stage-4", ("value", Stage4Delay)));
                    fusionReactor.MeltdownAnnouncements &= ~FusionReactorMeltdownStage.SafeStages;
                    fusionReactor.MeltdownAnnouncements |= FusionReactorMeltdownStage.Stage4;
                }
                break;

            default:
                break;
        }

        return;

        void IntegrityUpdate()
        {
            if (MathF.Abs(fusionReactor.IntegrityRatio - fusionReactor.LastAnnouncedIntegrity) < fusionReactor.AnnouncementInterval)
                return;

            var message = fusionReactor.IntegrityRatio >= fusionReactor.LastAnnouncedIntegrity ?
                Loc.GetString("fusion-reactor-controller-radio-integrity-rising", ("value", fusionReactor.IntegrityRatio)) :
                Loc.GetString("fusion-reactor-controller-radio-integrity-falling", ("value", fusionReactor.IntegrityRatio));

            SendRadioMessage(fusionReactor, message);
            fusionReactor.LastAnnouncedIntegrity = fusionReactor.IntegrityRatio;
        }

        void MeltdownAverted()
        {
            SendAnnouncement(fusionReactor, Loc.GetString("fusion-reactor-controller-announcement-stage-safe"));
            fusionReactor.MeltdownAnnouncements = FusionReactorMeltdownStage.SafeStages;
        }
    }

    private void SendRadioMessage(FusionReactorNodeGroup fusionReactor, string message)
    {
        if (!fusionReactor.MasterController.HasValue)
            return;

        var (uid, controller) = fusionReactor.MasterController.Value;

        foreach (var channelID in controller.AlertChannel)
        {
            if (!_protoMan.TryIndex<RadioChannelPrototype>(channelID, out var channel))
                continue;

            _radioSystem.SendRadioMessage(uid, message, channel, uid);
        }
    }

    private void SendAnnouncement(FusionReactorNodeGroup fusionReactor, string message, string? sender = null, SoundPathSpecifier? sound = null)
    {
        if (_gameTiming.CurTime < fusionReactor.NextAllowedAnnouncement)
            return;

        fusionReactor.NextAllowedAnnouncement = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(MathF.Min(Stage3Delay, Stage4Delay) - 1));

        if (!fusionReactor.MasterController.HasValue)
            return;

        var uid = fusionReactor.MasterController.Value.Owner;

        var stationUid = _station.GetStationInMap(Transform(uid).MapID);
        _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, message, sender ?? Loc.GetString("fusion-reactor-controller-announcement-sender"), false, sound, Color.Orange);
    }
    #endregion

    #region BUI
    private void UpdateControllerUI(EntityUid uid, FusionReactorControllerComponent comp)
    {
        if (!_uiSystem.IsUiOpen(uid, FusionReactorUiKey.Key))
            return;

        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        TryComp<FusionReactorPowerSupplyComponent>(fusionReactor.MasterController?.Owner, out var supplyComponent);

        _uiSystem.SetUiState(uid, FusionReactorUiKey.Key, new FusionReactorControllerBuiState()
        {
            Plasma = new(fusionReactor.Plasma),
            Stored = fusionReactor.Stored.Atoms,
            RequestedMagneticPressure = fusionReactor.RequestedMagneticPressure,
            Masers = fusionReactor.Masers.ToDictionary(m => GetNetEntity(m.Owner), m => GetBuiState(m.Owner, m.Comp)),
            Batteries = fusionReactor.Batteries.Select(b => (GetNetEntity(b.Owner), GetBuiState(b.Owner, b.Comp))).ToList(),
            MagnetTemperature = fusionReactor.Magnets.Count > 0 ? fusionReactor.Magnets.Average(m => m.Comp.Temperature) : Atmospherics.T20C,
            MagnetCritical = fusionReactor.Magnets.Count > 0 ? fusionReactor.Magnets.Average(m => m.Comp.TC) : 0,
            Integrity = fusionReactor.IntegrityRatio,
            Stability = fusionReactor.PlasmaStability,

            MeltdownStage = fusionReactor.MeltdownStage,
            CanEject = fusionReactor.CanEject,
            EventTime = fusionReactor.NextEventTime,

            IsMaster = fusionReactor.MasterController.HasValue && uid == fusionReactor.MasterController.Value.Owner,
            ExtractMode = comp.ExtractMode,
            WattSetting = comp.WattSetting,
            TempSetting = comp.TempSetting,
            Transfers = comp.Transfers,

            PowerExtracted = supplyComponent != null && supplyComponent.Enabled ? supplyComponent.Supply : 0,
            PowerExported = supplyComponent != null && supplyComponent.Enabled ? supplyComponent.Surplus : 0,
        });
    }

    private void OnControllerUIOpened(EntityUid uid, FusionReactorControllerComponent comp, ref BoundUIOpenedEvent args) => UpdateControllerUI(uid, comp);

    private void OnControllerSetInjectMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerSetInjectMessage args) =>
        comp.Transfers[args.Atom] = args.Data;

    private void OnControllerSetExtractMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerSetExtractMessage args)
    {
        comp.ExtractMode = args.Mode;
        comp.WattSetting = args.WattSetting;
        comp.TempSetting = args.TempSetting;
    }

    private void OnControllerSetPressureMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerSetPressureMessage args) =>
        comp.RequestedMagneticPressure = args.RequestedMagneticPressure;

    private void OnControllerSetMaserPowerMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerSetMaserPowerMessage args)
    {
        var maserUid = GetEntity(args.Maser);
        if (!TryComp<FusionReactorMaserComponent>(maserUid, out var maserComp))
            return;

        var message = args.Message;
        OnMaserPowerMessage(maserUid, maserComp, ref message);
    }

    private void OnControllerSetMaserInjectMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerSetMaserInjectMessage args)
    {
        var maserUid = GetEntity(args.Maser);
        if (!TryComp<FusionReactorMaserComponent>(maserUid, out var maserComp))
            return;

        var message = args.Message;
        OnMaserInjectMessage(maserUid, maserComp, ref message);
    }

    private void OnControllerEditInjectMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerEditInjectMessage args)
    {
        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        var dict = fusionReactor.Stored.Atoms;

        if (dict.TryGetValue(args.Atom, out var count))
        {
            if (MathHelper.CloseTo(count, 0, FusionConsts.MinQuantity))
                dict.Remove(args.Atom);
        }
        else
        {
            dict[args.Atom] = 0;
        }
    }

    private void OnControllerEjectMessage(EntityUid uid, FusionReactorControllerComponent comp, ref FusionReactorControllerEjectMessage args)
    {
        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        if (!TryEjectCore(fusionReactor))
            SendRadioMessage(fusionReactor, $"Core eject failed, detonation in {(fusionReactor.NextEventTime - _gameTiming.CurTime).TotalSeconds:0.#} seconds");
    }

    #endregion
}
