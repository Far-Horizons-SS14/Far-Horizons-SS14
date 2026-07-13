using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.Ame.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;

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
            SetOnSatisfy(uid, () => _fusionSystem.AddJoule(fusionReactor.Plasma, GetPowerSupplied(uid) * dt));

            if (comp.InjectAntimatter)
            {
                var injectAM = dt * comp.InjectionRate;

                if (injectAM > comp.Antimatter)
                {
                    if (TryComp<AmeFuelContainerComponent>(comp.AMJarSlot.Item, out var fuelContainer) && fuelContainer.FuelAmount > 0)
                    {
                        fuelContainer.FuelAmount -= 1;
                        comp.Antimatter += 1;
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
                    fusionReactor.Plasma.ChangeAtom(new(-1, 0), injectAM * 1e-7);
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

        _uiSystem.SetUiState(uid, FusionReactorUiKey.Key,
           new FusionReactorMaserBuiState
           {
               PowerSetting = maser.PowerSetting,
               MaxPowerSetting = maser.MaxPowerSetting,

               AMInjection = maser.InjectAntimatter,
               AMJar = EntityManager.GetNetEntity(maser.AMJarSlot.Item),
           });
    }
}