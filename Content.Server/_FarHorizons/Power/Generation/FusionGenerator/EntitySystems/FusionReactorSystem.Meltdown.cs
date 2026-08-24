using System.Linq;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Shared.Atmos;
using Content.Shared.Light.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void ProcessDamage(FusionReactorNodeGroup fusionReactor, float dt)
    {
        var plasma = fusionReactor.Plasma;
        var pressure = (float)plasma.ConstrainedPressure;

        if (!float.IsFinite(pressure))
            return;

        var temperature = (float)plasma.Temperature;

        if (pressure <= 0)
        {
            fusionReactor.Integrity += MathF.Min(fusionReactor.IntegrityRegeneration * dt, fusionReactor.IntegrityMax - fusionReactor.Integrity);
        }
        else
        {
            fusionReactor.Integrity -= MathF.Min((MathF.Pow(pressure, 1 / fusionReactor.ResistancePressure) - 0.5f) * (MathF.Pow(temperature, 1 / fusionReactor.ResistanceTemperature) - 0.5f) * dt, fusionReactor.IntegrityMaxDecay * dt);
        }
    }

    private void UpdateMeltdownStage(FusionReactorNodeGroup fusionReactor)
    {
        var previousStage = fusionReactor.MeltdownStage;
        fusionReactor.MeltdownStage = fusionReactor.IntegrityRatio switch
        {
            float n when n is >= 1
                => FusionReactorMeltdownStage.Stage0,
            float n when n is < 1 and > 0.25f
                => FusionReactorMeltdownStage.Stage1,
            float n when n is <= 0.25f and > 0f
                => FusionReactorMeltdownStage.Stage2,
            float n when n <= 0 && fusionReactor.MeltdownStage != FusionReactorMeltdownStage.Stage4
                => FusionReactorMeltdownStage.Stage3,
            float n when n <= 0 && fusionReactor.MeltdownStage == FusionReactorMeltdownStage.Stage4
                => FusionReactorMeltdownStage.Stage4,
            _
                => FusionReactorMeltdownStage.Stage0,
        };

        fusionReactor.CanEject = fusionReactor.MeltdownStage == FusionReactorMeltdownStage.Stage3;

        if (!fusionReactor.HasDumpedCoolant && fusionReactor.IntegrityRatio <= 0.05)
            EmergencyCoolantDump(fusionReactor);

        if (_gameTiming.CurTime >= fusionReactor.NextEventTime)
        {
            switch (fusionReactor.MeltdownStage)
            {
                case FusionReactorMeltdownStage.Stage3:
                    if (AllowMassDestruction)
                    {
                        fusionReactor.MeltdownStage = FusionReactorMeltdownStage.Stage4;
                    }
                    else
                    {
                        CatastrophicFailure(fusionReactor);
                    }
                    break;

                case FusionReactorMeltdownStage.Stage4:
                    CatastrophicFailure(fusionReactor);
                    break;

                default:
                    break;
            }
        }

        if (previousStage != fusionReactor.MeltdownStage)
        {
            fusionReactor.NextEventTime = fusionReactor.MeltdownStage switch
            {
                FusionReactorMeltdownStage.Stage3
                    => _gameTiming.CurTime.Add(TimeSpan.FromSeconds(Stage3Delay)),
                FusionReactorMeltdownStage.Stage4
                    => _gameTiming.CurTime.Add(TimeSpan.FromSeconds(Stage4Delay)),
                _
                    => TimeSpan.MaxValue,
            };

            if (fusionReactor.MeltdownStage < FusionReactorMeltdownStage.Stage2)
                fusionReactor.HasDumpedCoolant = false;
        }
    }

    private void EmergencyCoolantDump(FusionReactorNodeGroup fusionReactor)
    {
        fusionReactor.HasDumpedCoolant = true;

        GasMixture coolant = new();
        _atmosphereSystem.Merge(coolant, fusionReactor.CoolantIn);
        _atmosphereSystem.Merge(coolant, fusionReactor.CoolantOut);

        fusionReactor.CoolantIn.Clear();
        fusionReactor.CoolantOut.Clear();

        var coolantHeatCap = _atmosphereSystem.GetHeatCapacity(coolant, true);
        var plasmaHeatCap = _fusionSystem.GetHeatCapacity(fusionReactor.Plasma);

        // Math doesn't really work if there's nothing there
        if (plasmaHeatCap != 0 && coolantHeatCap != 0)
        {
            var coolantThermal = _atmosphereSystem.GetThermalEnergy(coolant);
            var plasmaThermal = _fusionSystem.GetThermalEnergy(fusionReactor.Plasma);

            // Positive means plasma has more energy, negative means coolant has more energy
            var dE = plasmaThermal - coolantThermal;

            var portion = dE * 0.5;

            _fusionSystem.ChangeJoule(fusionReactor.Plasma, -portion);
            _atmosphereSystem.AddHeat(coolant, (float)portion);
        }

        var dumpPortions = Enumerable.Range(0, fusionReactor.Magnets.Count).Select(i => new GasMixture(1000)).ToList();
        _atmosphereSystem.DivideInto(coolant, dumpPortions);

        for (var i = 0; i < fusionReactor.Magnets.Count; i++)
        {
            var magnet = fusionReactor.Magnets[i];
            var tilemix = _atmosphereSystem.GetTileMixture(magnet.Owner, true);
            if (tilemix == null)
                continue;

            _atmosphereSystem.Merge(tilemix, dumpPortions[i]);
            dumpPortions[i].Clear();
        }

        coolant.Clear();
    }

    private void CatastrophicFailure(FusionReactorNodeGroup fusionReactor)
    {
        fusionReactor.NextEventTime = TimeSpan.MaxValue;

        var coords = GetCenter(fusionReactor);

        var fusionMixture = fusionReactor.Plasma;
        var pressure = (float)(fusionMixture.Pressure + fusionMixture.ConstrainedPressure);
        var volume = (float)Math.Min(fusionMixture.ConstrainedVolume, fusionMixture.Volume);

        /// joules of energy resultant from it no longer being under pressure
        var energy = pressure * volume / 0.67f * (1 - MathF.Pow(Atmospherics.OneAtmosphere * 1000 / pressure, 0.67f / 1.67f));
        /// joules of energy contained within the mixture
        var mixEnergy = MathF.Cbrt((float)_fusionSystem.GetThermalEnergy(fusionReactor.Plasma));

        var intensity = Math.Clamp(energy + mixEnergy, ExplosiveForceMin, AllowMassDestruction ? ExplosiveForceMaxDestruction : ExplosiveForceMax);

        // Last ditch input verification to catch strange edge cases, the explosion system breaks if this isn't done
        if (!float.IsFinite(intensity))
            intensity = ExplosiveForceMin;

        _explosionSystem.QueueExplosion(coords, "Default", intensity, 5, 100, null);
        _empSystem.EmpPulse(coords, MathF.Cbrt((float)fusionReactor.MagneticPressure), 10000, TimeSpan.FromSeconds(15));

        foreach (var node in fusionReactor.Nodes)
        {
            QueueDel(node.Owner);
        }
    }

    private bool TryEjectCore(FusionReactorNodeGroup fusionReactor)
    {
        if (!fusionReactor.CanEject)
            return false;

        fusionReactor.CanEject = false;

        var graceSeconds = Stage3Delay + (AllowMassDestruction ? Stage4Delay : 0);
        fusionReactor.NextEventTime = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(graceSeconds));
        fusionReactor.MeltdownStage = FusionReactorMeltdownStage.Stage4;

        if (fusionReactor.IntegrityRatio < -1)
            return false;

        var position = GetCenter(fusionReactor);
        var delayMS = (int)Math.Clamp(fusionReactor.NextEventTime.Subtract(_gameTiming.CurTime).TotalMilliseconds, 1000, Stage3Delay * 1000);
        Timer.Spawn(delayMS, () => EjectedCoreEffect(position));

        EntityUid? grid = null;
        List<(Vector2i, Tile)> tiles = [];
        foreach (var node in fusionReactor.Nodes)
        {
            grid ??= _transformSystem.GetGrid(node.Owner);

            // May end up deleting the tile at (0,0) on some grid, but something would have to go very wrong for that to happen
            tiles.Add((_transformSystem.GetGridTilePositionOrDefault(node.Owner), Tile.Empty));
            QueueDel(node.Owner);
        }

        if (TryComp(grid, out MapGridComponent? gridComponent))
            _mapSystem.SetTiles(grid.Value, gridComponent, tiles);

        return true;
    }

    private void EjectedCoreEffect(MapCoordinates epicenter)
    {
        /// Screen rumble
        var playerFilter = Filter.Empty();
        playerFilter.AddInMap(epicenter.MapId);
        foreach (var player in playerFilter.Recipients)
        {
            if (!player.AttachedEntity.HasValue)
                continue;

            EnsureComp<FusionReactorCameraShakeComponent>(player.AttachedEntity.Value);
        }

        /// Explosion sound
        /// Despite the volume being 20, it's not really all that loud
        SoundSpecifier explosionSound = new SoundCollectionSpecifier("ExplosionFar", AudioParams.Default.WithVolume(20f).WithPitchScale(0.75f));
        _audioSystem.PlayGlobal(explosionSound, playerFilter, true, explosionSound.Params);

        /// Light flash
        if (_mapSystem.MapExists(epicenter.MapId))
        {
            var lightEntity = Spawn("FusionReactorEjectFlash", epicenter);
        }

        /// Light flickers
        var lightQuery = EntityQueryEnumerator<TransformComponent, PoweredLightComponent>();
        while (lightQuery.MoveNext(out var uid, out var xform, out var _))
        {
            if (xform.MapID != epicenter.MapId)
                continue;

            _ghostSystem.DoGhostBooEvent(uid);
        }

        /// TODO: nebula on skybox
    }
}