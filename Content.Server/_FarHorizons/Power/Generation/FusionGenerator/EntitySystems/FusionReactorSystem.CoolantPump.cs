using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    private void CoolingInitialize()
    {
        SubscribeLocalEvent<FusionReactorCoolantPumpComponent, AtmosDeviceUpdateEvent>(OnCoolantPumpUpdate);
        SubscribeLocalEvent<FusionReactorCoolantPumpComponent, GasAnalyzerScanEvent>(OnCoolantPumpAnalyze);

        SubscribeLocalEvent<FusionReactorCoolantPumpComponent, FusionReactorCoolantPumpSetEnableMessage>(OnCoolantPumpSetEnableMessage);
        SubscribeLocalEvent<FusionReactorCoolantPumpComponent, FusionReactorCoolantPumpSetFlowMessage>(OnCoolantPumpSetFlowMessage);
        SubscribeLocalEvent<FusionReactorCoolantPumpComponent, BoundUIOpenedEvent>(OnCoolantPumpUIOpened);
    }

    private void OnCoolantPumpAnalyze(EntityUid uid, FusionReactorCoolantPumpComponent comp, ref GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= [];

        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        if (!_nodeContainer.TryGetNode(uid, comp.PipeName, out PipeNode? pipe))
            return;

        var sourceMix = comp.IsInlet ? pipe.Air : fusionReactor.CoolantOut;
        var receiverMix = comp.IsInlet ? fusionReactor.CoolantIn : pipe.Air;

        if (sourceMix.Volume != 0f)
        {
            var inletAirLocal = sourceMix.Clone();
            if (comp.IsInlet)
            {
                inletAirLocal.Multiply(pipe.Volume / pipe.Air.Volume);
                inletAirLocal.Volume = pipe.Volume;
            }
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
        }

        if (receiverMix.Volume != 0f)
        {
            var outletAirLocal = receiverMix.Clone();
            if (!comp.IsInlet)
            {
                outletAirLocal.Multiply(pipe.Volume / pipe.Air.Volume);
                outletAirLocal.Volume = pipe.Volume;
            }
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
        }
    }

    private void OnCoolantPumpUpdate(EntityUid uid, FusionReactorCoolantPumpComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        SetPowerDraw(uid, comp.Enabled);
        if (!comp.Enabled)
            return;

        if (!TryGetReactorGroup(uid, out var fusionReactor))
            return;

        if (!_nodeContainer.TryGetNode(uid, comp.PipeName, out PipeNode? pipe))
            return;

        var sourceMix = comp.IsInlet ? pipe.Air : fusionReactor.CoolantOut;
        var receiverMix = comp.IsInlet ? fusionReactor.CoolantIn : pipe.Air;

        var satisfaction = GetPowerSatisfaction(uid);

        if (satisfaction <= 0)
            return;

        var transferVolume = CalculateTransferVolume(comp.FlowRate * satisfaction, sourceMix, receiverMix, args.dt);
        var transferMix = sourceMix.RemoveVolume(transferVolume);

        _atmosphereSystem.Merge(receiverMix, transferMix);
        
        Dirty(uid, comp);
    }

    private float CalculateTransferVolume(float volume, GasMixture inlet, GasMixture outlet, float dt)
    {
        var wantToTransfer = volume * _atmosphereSystem.PumpSpeedup() * dt;
        var transferVolume = Math.Min(inlet.Volume, wantToTransfer);
        var transferMoles = inlet.Pressure * transferVolume / (inlet.Temperature * Atmospherics.R);
        var molesSpaceLeft = ((Atmospherics.MaxOutputPressure * 3) - outlet.Pressure) * outlet.Volume / (outlet.Temperature * Atmospherics.R);
        var actualMolesTransfered = Math.Clamp(transferMoles, 0, Math.Max(0, molesSpaceLeft));
        return Math.Max(0, actualMolesTransfered * inlet.Temperature * Atmospherics.R / inlet.Pressure);
    }

    private void OnCoolantPumpSetEnableMessage(EntityUid uid, FusionReactorCoolantPumpComponent comp, ref FusionReactorCoolantPumpSetEnableMessage args) =>
        comp.Enabled = args.Enable;

    private void OnCoolantPumpSetFlowMessage(EntityUid uid, FusionReactorCoolantPumpComponent comp, ref FusionReactorCoolantPumpSetFlowMessage args) =>
        comp.FlowRate = args.FlowRate;

    private void OnCoolantPumpUIOpened(EntityUid uid, FusionReactorCoolantPumpComponent comp, ref BoundUIOpenedEvent args)
    {

    }
}