using System.Diagnostics.CodeAnalysis;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Shared.Camera;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FusionSystem _fusionSystem = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _soundSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    /// <summary>
    /// May eventually be handled by a grid/map level component like the atmosphere system, but for now the system can keep track of it.
    /// </summary>
    private readonly List<FusionReactorNodeGroup> _fusionReactors = [];

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();

        BatteryInitialize();
        ControllerInitialize();
        TorusInitialize();
        CoolingInitialize();
        MaserInitialize();
        GasInletInitialize();
    }

    public void AddReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Add(nodeGroup);

    public void RemoveReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Remove(nodeGroup);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var batteryQuery = EntityQueryEnumerator<FusionReactorBatteryComponent>();
        while (batteryQuery.MoveNext(out var uid, out var battery))
        {
            // Battery UIs update every tick, just like a normal battery
            UpdateBatteryUI(uid, battery);
        }

        var shakeQuery = EntityQueryEnumerator<FusionReactorCameraShakeComponent>();
        while (shakeQuery.MoveNext(out var uid, out var shake))
        {
            if(_gameTiming.CurTime < shake.NextShake)
                continue;
            
            UpdateEffectShake(uid, shake);
        }

        foreach (var reactor in _fusionReactors)
        {
            // TODO: not every update
            ProcessReactor(reactor);
        }
    }

    private void ProcessReactor(FusionReactorNodeGroup fusionReactor)
    {
        var time = _gameTiming.CurTime;
        var dt = (float)(time - fusionReactor.LastProcess).TotalSeconds;
        fusionReactor.LastProcess = time;

        ProcessCooling(fusionReactor, dt);
        ProcessMagnetics(fusionReactor, dt);

        ProcessMaser(fusionReactor, dt);

        ProcessInjects(fusionReactor, dt);
        ExtractPower(fusionReactor, dt);

        ProcessPowerDraws(fusionReactor, dt);

        _fusionSystem.React(fusionReactor.Plasma, dt);

        ProcessDamage(fusionReactor, dt);
        UpdateMeltdownStage(fusionReactor);
        
        UpdateRadio(fusionReactor);
    }

    private bool TryGetReactorGroup(EntityUid uid, [NotNullWhen(true)] out FusionReactorNodeGroup? reactorNodeGroup)
    {
        reactorNodeGroup = null;
        if (!EntityManager.TryGetComponent<NodeContainerComponent>(uid, out var nodeContainer))
            return false;

        if (!nodeContainer.Nodes.TryGetValue("reactor", out var node))
            return false;

        if (node.NodeGroup is not FusionReactorNodeGroup nodeGroup || nodeGroup == null)
            return false;

        reactorNodeGroup = nodeGroup;
        return true;
    }

    private MapCoordinates GetCenter(FusionReactorNodeGroup fusionReactor)
    {
        MapId? map = null;
        var ymax = float.NegativeInfinity;
        var ymin = float.PositiveInfinity;
        var xmax = float.NegativeInfinity;
        var xmin = float.PositiveInfinity;

        foreach (var node in fusionReactor.Nodes)
        {
            var coord = _transformSystem.GetMapCoordinates(node.Owner);

            map ??= coord.MapId;
            ymax = MathF.Max(ymax, coord.Y);
            ymin = MathF.Min(ymin, coord.Y);
            xmax = MathF.Max(xmax, coord.X);
            xmin = MathF.Min(xmin, coord.X);
        }

        return map == null ? new() : new((xmax + xmin) / 2, (ymax + ymin) / 2, map.Value);
    }
}