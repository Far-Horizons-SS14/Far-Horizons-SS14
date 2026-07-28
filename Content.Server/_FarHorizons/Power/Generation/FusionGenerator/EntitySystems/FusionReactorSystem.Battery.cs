using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    private void BatteryInitialize()
    {
        SubscribeLocalEvent<FusionReactorBatteryComponent, RefreshChargeRateEvent>(OnBatteryRefreshChargeRate);
        SubscribeLocalEvent<FusionReactorBatteryComponent, ComponentStartup>(OnBatteryStartup);

        SubscribeLocalEvent<FusionReactorBatteryComponent, FusionReactorBatterySetMaxInputMessage>(OnBatterySetMaxInputMessage);
        SubscribeLocalEvent<FusionReactorBatteryComponent, FusionReactorBatterySetCanChargeMessage>(OnBatterySetCanChargeMessage);
        SubscribeLocalEvent<FusionReactorBatteryComponent, FusionReactorBatterySetCanDischargeMessage>(OnBatterySetCanDischargeMessage);

        SubscribeLocalEvent<FusionReactorPowerDrawComponent, ExaminedEvent>(OnPowerDrawExamined);
        SubscribeLocalEvent<FusionReactorPowerSupplyComponent, ExaminedEvent>(OnPowerSupplyExamined);
    }

    private void OnBatteryRefreshChargeRate(EntityUid uid, FusionReactorBatteryComponent comp, ref RefreshChargeRateEvent args)
    {
        if (comp.NetBattery.CanCharge)
            args.NewChargeRate += comp.Supply;

        if (comp.NetBattery.CanDischarge)
            args.NewChargeRate -= comp.Demand;
    }

    private void OnBatteryStartup(EntityUid uid, FusionReactorBatteryComponent comp, ref ComponentStartup args)
    {
        comp.NetBattery = EnsureComp<PowerNetworkBatteryComponent>(uid);
        comp.Battery = EnsureComp<BatteryComponent>(uid);
    }

    private void OnPowerDrawExamined(EntityUid uid, FusionReactorPowerDrawComponent comp, ref ExaminedEvent args)
    {
        if (!comp.Enabled)
            return;

        if (!Comp<TransformComponent>(uid).Anchored || !args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FusionReactorPowerDrawComponent)))
        {
            args.PushMarkup(Loc.GetString("fusion-reactor-powerdraw-examine", ("supply", comp.Supplied), ("demand", comp.Draw)));
        }
    }

    private void OnPowerSupplyExamined(EntityUid uid, FusionReactorPowerSupplyComponent comp, ref ExaminedEvent args)
    {
        if (!comp.Enabled)
            return;

        if (!Comp<TransformComponent>(uid).Anchored || !args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FusionReactorPowerSupplyComponent)))
        {
            args.PushMarkup(Loc.GetString("fusion-reactor-powersupply-examine-gen", ("supply", comp.Supply)));
            args.PushMarkup(Loc.GetString("fusion-reactor-powersupply-examine-out", ("surplus", comp.Surplus)));
        }
    }

    #region BUI
    private void UpdateBatteryUi(EntityUid uid, FusionReactorBatteryComponent comp)
    {
        if (!_uiSystem.IsUiOpen(uid, FusionReactorUiKey.Key))
            return;

        _uiSystem.SetUiState(uid, FusionReactorUiKey.Key, GetBuiState(uid, comp));
    }

    public FusionReactorBatteryBuiState GetBuiState(EntityUid uid, FusionReactorBatteryComponent battery) => new()
    {
        Charge = _battery.GetCharge(uid),
        MaxCharge = battery.Battery.MaxCharge,
        ExternalInput = battery.NetBattery.CurrentReceiving,
        MaxExternalInput = battery.NetBattery.MaxChargeRate,
        MaxMaxExternalInput = battery.MaxMaxExternalInput,
        MinMaxExternalInput = battery.MinMaxExternalInput,
        Efficiency = battery.NetBattery.Efficiency,
        Input = battery.Supply,
        Output = battery.Demand,
        CanCharge = battery.NetBattery.CanCharge,
        CanDischarge = battery.NetBattery.CanDischarge,
    };

    private void OnBatterySetMaxInputMessage(EntityUid uid, FusionReactorBatteryComponent comp, ref FusionReactorBatterySetMaxInputMessage args) =>
        comp.NetBattery.MaxChargeRate = args.MaxExternalInput;

    private void OnBatterySetCanChargeMessage(EntityUid uid, FusionReactorBatteryComponent comp, ref FusionReactorBatterySetCanChargeMessage args) =>
        comp.NetBattery.CanCharge = args.CanCharge;

    private void OnBatterySetCanDischargeMessage(EntityUid uid, FusionReactorBatteryComponent comp, ref FusionReactorBatterySetCanDischargeMessage args) =>
        comp.NetBattery.CanDischarge = args.CanDischarge;
    #endregion
}