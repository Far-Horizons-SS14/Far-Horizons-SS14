using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.Ame.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    /// Not arbitrary
    /// A one core AME running at 1 (or 2) injection rate gives the most energy possible per unit of antimatter at 602059.9913 kJ. Using E=mc^2, that 
    /// equates to roughly 6.6988e-12 kg. When halved to account for the equal amount of matter consumed, the resulting amount of antimatter per unit
    /// is 3.3494e-12 kg. Assuming the antimatter is pure positrons, there are 1.9973e15 positrons/unit, or 3.3206e-9 mol/unit.
    private const double AntimatterPerUnit = 3.3206373689e-9;

    private void MaserInitialize()
    {
        SubscribeLocalEvent<FusionReactorMaserComponent, ComponentInit>(OnMaserInit);
        SubscribeLocalEvent<FusionReactorMaserComponent, ComponentRemove>(OnMaserRemove);

        SubscribeLocalEvent<FusionReactorMaserComponent, FusionReactorMaserSetInjectionMessage>(OnMaserInjectMessage);
        SubscribeLocalEvent<FusionReactorMaserComponent, FusionReactorMaserSetPowerMessage>(OnMaserPowerMessage);
        SubscribeLocalEvent<FusionReactorMaserComponent, BoundUIOpenedEvent>(OnMaserUIOpened);
    }

    private void OnMaserInit(EntityUid uid, FusionReactorMaserComponent comp, ref ComponentInit args) => _slotsSystem.AddItemSlot(uid, FusionReactorMaserComponent.AMJarSlotId, comp.AMJarSlot);

    private void OnMaserRemove(EntityUid uid, FusionReactorMaserComponent comp, ref ComponentRemove args) => _slotsSystem.RemoveItemSlot(uid, comp.AMJarSlot);

    private void ProcessMaser(FusionReactorNodeGroup fusionReactor, float dt)
    {
        foreach (var (uid, comp) in fusionReactor.Masers)
        {
            SetPowerDraw(uid, comp.PowerSetting > 0, comp.BasePower * MathF.Pow(comp.PowerExponent, comp.PowerSetting - 1));
            SetOnSatisfy(uid, () => _fusionSystem.ChangeJoule(fusionReactor.Plasma, GetPowerSupplied(uid) * dt));

            if (comp.InjectAntimatter)
            {
                var injectAM = dt * comp.InjectionRate;

                if (injectAM > comp.Antimatter)
                {
                    if (TryComp<AmeFuelContainerComponent>(comp.AMJarSlot.Item, out var fuelContainer) && fuelContainer.FuelAmount > 0)
                    {
                        var transfer = Math.Min((int)Math.Floor(injectAM + 1), fuelContainer.FuelAmount);
                        fuelContainer.FuelAmount -= transfer;
                        comp.Antimatter += transfer;
                    }
                    else
                    {
                        injectAM = comp.Antimatter;
                        comp.InjectAntimatter = false;
                    }
                }

                if (injectAM > 0 && comp.Antimatter > 0)
                {
                    comp.Antimatter -= injectAM;
                    fusionReactor.Plasma.ChangeAtom(new(-1, 0), injectAM * AntimatterPerUnit);
                }
            }

            UpdateMaserUI(uid, comp);
        }
    }

    private void OnMaserPowerMessage(EntityUid uid, FusionReactorMaserComponent comp, ref FusionReactorMaserSetPowerMessage args) =>
        comp.PowerSetting = Math.Clamp(args.PowerLevel, 0, comp.MaxPowerSetting);

    private void OnMaserInjectMessage(EntityUid uid, FusionReactorMaserComponent comp, ref FusionReactorMaserSetInjectionMessage args) =>
        comp.InjectAntimatter = args.InjectAM;

    private void OnMaserUIOpened(EntityUid uid, FusionReactorMaserComponent comp, ref BoundUIOpenedEvent args) =>
        UpdateMaserUI(uid, comp);

    public void UpdateMaserUI(EntityUid uid, FusionReactorMaserComponent maser)
    {
        if (!_uiSystem.IsUiOpen(uid, FusionReactorUiKey.Key))
            return;

        _uiSystem.SetUiState(uid, FusionReactorUiKey.Key, GetBuiState(uid, maser));
    }

    public FusionReactorMaserBuiState GetBuiState(EntityUid uid, FusionReactorMaserComponent maser) => new()
    {
        PowerSetting = maser.PowerSetting,
        MaxPowerSetting = maser.MaxPowerSetting,

        AMInjection = maser.InjectAntimatter,
        AMJar = EntityManager.GetNetEntity(maser.AMJarSlot.Item),
        Antimatter = TryComp<AmeFuelContainerComponent>(maser.AMJarSlot.Item, out var fuelContainer) ? fuelContainer.FuelAmount : 0,

        RequestedPower = TryComp<FusionReactorPowerDrawComponent>(uid, out var powerDrawComponent) && powerDrawComponent.Enabled ? powerDrawComponent.Draw : 0,
        ReceivedPower = GetPowerSupplied(uid),
    };
}