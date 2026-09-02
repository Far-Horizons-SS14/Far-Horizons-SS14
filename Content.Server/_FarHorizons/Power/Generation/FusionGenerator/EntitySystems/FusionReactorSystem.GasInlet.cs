using System.Linq;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void GasInletInitialize()
    {
        SubscribeLocalEvent<FusionReactorGasInletComponent, AtmosDeviceUpdateEvent>(OnGasInletUpdate);

        SubscribeLocalEvent<FusionReactorGasInletComponent, FusionReactorGasInletSetEnableMessage>(OnGasInletSetEnableMessage);
        SubscribeLocalEvent<FusionReactorGasInletComponent, FusionReactorGasInletSetPowerMessage>(OnGasInletSetPowerMessage);
        SubscribeLocalEvent<FusionReactorGasInletComponent, BoundUIOpenedEvent>(OnGasInletUIOpened);
    }

    private void OnGasInletUpdate(EntityUid uid, FusionReactorGasInletComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!comp.Enabled)
        {
            comp.Production.Clear();
            SetPowerDraw(uid, comp.Enabled);
            Dirty(uid, comp);
            return;
        }

        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        if (!_nodeContainer.TryGetNode(uid, comp.PipeName, out PipeNode? pipe))
            return;

        var gasTemp = pipe.Air.Temperature;
        // Not using GetThermalEnergy for the sake of consistancy
        var therm = _atmosphereSystem.GetHeatCapacity(pipe.Air, true) * gasTemp;
        var targetTherm = _atmosphereSystem.GetHeatCapacity(pipe.Air, true) * FusionConsts.PlasmaTemperature;
        var deltaTherm = targetTherm - therm;

        var targetDraw = Math.Min(deltaTherm / args.dt, comp.PowerSetting);

        SetPowerDraw(uid, deltaTherm > 0, targetDraw);

        var portion = deltaTherm <= 0 ? 1 : args.dt * targetDraw / deltaTherm * GetPowerSatisfaction(uid);

        var convertGas = pipe.Air.RemoveRatio(portion);
        var fusionMix = _fusionSystem.ConvertFromGasMixture(convertGas);
        fusionMix.Temperature = Math.Max(gasTemp, FusionConsts.PlasmaTemperature);

        var dt = args.dt;
        comp.Production = [.. fusionMix.Atoms.Select(kvp => (kvp.Key, kvp.Value / dt))];

        Dirty(uid, comp);

        _fusionSystem.Merge(fusionReactor.Stored, fusionMix);
    }

    private void OnGasInletSetEnableMessage(EntityUid uid, FusionReactorGasInletComponent comp, ref FusionReactorGasInletSetEnableMessage args) =>
        comp.Enabled = args.Enable;

    private void OnGasInletSetPowerMessage(EntityUid uid, FusionReactorGasInletComponent comp, ref FusionReactorGasInletSetPowerMessage args) =>
        comp.PowerSetting = args.PowerLevel;

    private void OnGasInletUIOpened(EntityUid uid, FusionReactorGasInletComponent comp, ref BoundUIOpenedEvent args) =>
        UpdateGasInletUI(uid, comp);

    public void UpdateGasInletUI(EntityUid uid, FusionReactorGasInletComponent inlet)
    {
        if (!_uiSystem.IsUiOpen(uid, FusionReactorUiKey.Key))
            return;

        _uiSystem.SetUiState(uid, FusionReactorUiKey.Key,
           new FusionReactorGasInletBuiState
           {
               PowerSetting = inlet.PowerSetting,
               MaxPowerSetting = inlet.MaxPowerSetting,

               Enabled = inlet.Enabled,
           });
    }
}