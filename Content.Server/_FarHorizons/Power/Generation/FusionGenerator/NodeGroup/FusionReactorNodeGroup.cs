using System.Linq;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;

[NodeGroup(NodeGroupID.FusionReactor)]
public sealed partial class FusionReactorNodeGroup : BaseNodeGroup
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    private FusionReactorSystem? _fusionReactorSystem;
    private AtmosphereSystem? _atmosphereSystem;
    private FusionSystem? _fusionSystem;

    [ViewVariables]
    public Entity<FusionReactorControllerComponent>? MasterController { get; private set; }

    private const float LitersPerTorus = 2500;
    private const float LitersPerMagnet = 200;

    [ViewVariables]
    public TimeSpan LastProcess = TimeSpan.Zero;

    /// <summary>
    /// Torus parts that count as magnets for the fusion reactor, similar to an AME's cores.
    /// </summary>
    public readonly List<Entity<FusionReactorTorusComponent>> Magnets = [];
    public int SuperconductingCount => Magnets.Count(m => m.Comp.Superconducting);

    /// <summary>
    /// Torus parts that aren't magnets.
    /// </summary>
    public readonly List<Entity<FusionReactorTorusComponent>> Torus = [];
    public int TorusCount => Torus.Count;

    /// <summary>
    /// The batteries attached to the fusion reactor
    /// </summary>
    public readonly List<Entity<FusionReactorBatteryComponent>> Batteries = [];

    /// <summary>
    /// The MASERs attached to the fusion reactor
    /// </summary>
    public readonly List<Entity<FusionReactorMaserComponent>> Masers = [];

    /// <summary>
    /// The power-producing plasma within the torus
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionMixture Plasma = new();

    /// <summary>
    /// Atoms in storage, waiting to be injected
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionMixture Stored = new(); // Doesn't need to be a FusionMixture, but it makes interacting with it a lot easier

    /// <summary>
    /// Coolent flowing into the reactor
    /// </summary>
    [ViewVariables]
    public GasMixture CoolantIn = new();
    /// <summary>
    /// Coolent flowing out of the reactor
    /// </summary>
    [ViewVariables]
    public GasMixture CoolantOut = new();

    /// <summary>
    /// The pressure exerted on the <see cref="Plasma"/> by magnets
    /// </summary>
    [ViewVariables]
    public double MagneticPressure { get; set; } = 1000;
    /// <summary>
    /// How much pressure the reactor is trying to exert
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequestedMagneticPressure { get; set; } = 1000;

    /// <summary>
    /// The stability of the plasma within the reactor
    /// </summary>
    /// <remarks>
    /// As stability decreases, actions like extracting power and adjusting contents should become more difficult
    /// </remarks>
    [ViewVariables]
    public float PlasmaStability => StabilityCalculation();

    /// <summary>
    /// The "health" of the reactor
    /// </summary>
    [DataField]
    public float Integrity = 100;

    /// <summary>
    /// The maximum value of <see cref="Integrity"/>
    /// </summary>
    [DataField]
    public float IntegrityMax = 100;

    /// <summary>
    /// Integrity of the reactor, expressed as a ratio of <see cref="Integrity"/> to <see cref="IntegrityMax"/>
    /// </summary>
    [ViewVariables]
    public float IntegrityRatio => Integrity / IntegrityMax;

    /// <summary>
    /// How much <see cref="Integrity"/> should be recovered per second
    /// </summary>
    [DataField]
    public float IntegrityRegeneration = 0.1f;

    /// <summary>
    /// The maximum amount <see cref="Integrity"/> can be removed per second
    /// </summary>
    [DataField]
    public float IntegrityMaxDecay = 0.5f;

    /// <summary>
    /// How resistant is the reactor to the pressure of a confinement failure
    /// </summary>
    /// <remarks>
    /// d = p ^ (1 / this) - 0.5
    /// </remarks>
    [DataField]
    public float ResistancePressure = 10;

    /// <summary>
    /// How resistant is the reactor to the temperature of a confinement failure
    /// </summary>
    /// <remarks>
    /// d = t ^ (1 / this) - 0.5
    /// </remarks>
    [DataField]
    public float ResistanceTemperature = 10;

    /// <summary>
    /// Current meltdown stage of the reactor
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionReactorMeltdownStage MeltdownStage = FusionReactorMeltdownStage.Stage0;

    /// <summary>
    /// What meltdown announcements have been sent
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionReactorMeltdownStage MeltdownAnnouncements = FusionReactorMeltdownStage.SafeStages;

    /// <summary>
    /// Last integrity ratio value announced over radio
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float LastAnnouncedIntegrity = 1;

    /// <summary>
    /// Minimum difference between <see cref="LastAnnouncedIntegrity"/> and <see cref="IntegrityRatio"/> before another announcement is made
    /// </summary>
    [DataField]
    public float AnnouncementInterval = 0.05f;

    /// <summary>
    /// The next time the reactor is allowed to make a station-wide announcement
    /// </summary>
    /// <remarks>
    /// Intended to prevent announcement spam
    /// </remarks>
    [ViewVariables]
    public TimeSpan NextAllowedAnnouncement = TimeSpan.Zero;

    /// <summary>
    /// Next time for something to happen, used for time-based events
    /// </summary>
    /// <remarks>
    /// If there is no event, should be set to TimeSpan.MaxValue
    /// </remarks>
    [ViewVariables]
    public TimeSpan NextEventTime = TimeSpan.MaxValue;

    /// <summary>
    /// Is a core eject attempt allowed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanEject = false;

    /// <summary>
    /// Has the reactor attempted an emergency coolant dump
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasDumpedCoolant = false;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _fusionSystem = entMan.EntitySysManager.GetEntitySystem<FusionSystem>();
        _fusionReactorSystem = entMan.EntitySysManager.GetEntitySystem<FusionReactorSystem>();
        _fusionReactorSystem.AddReactor(this);

        _atmosphereSystem = entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        EntityUid? gridEnt = null;

        var torusQuery = _entityManager.GetEntityQuery<FusionReactorTorusComponent>();
        var controllerQuery = _entityManager.GetEntityQuery<FusionReactorControllerComponent>();
        var batteryQuery = _entityManager.GetEntityQuery<FusionReactorBatteryComponent>();
        var maserQuery = _entityManager.GetEntityQuery<FusionReactorMaserComponent>();
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!xformQuery.TryGetComponent(nodeOwner, out var xform))
                continue;
            if (!_entityManager.TryGetComponent(xform.GridUid, out MapGridComponent? grid))
                continue;

            if (gridEnt == null)
                gridEnt = xform.GridUid;
            else if (gridEnt != xform.GridUid)
                continue;

            if (torusQuery.TryGetComponent(nodeOwner, out var torus))
            {
                LoadTorus(nodeOwner, torus, xform, grid, torusQuery);
                continue;
            }

            if (batteryQuery.TryGetComponent(nodeOwner, out var battery))
            {
                LoadBattery(nodeOwner, battery);
                continue;
            }

            if (maserQuery.TryGetComponent(nodeOwner, out var maser))
            {
                LoadMaser(nodeOwner, maser);
                continue;
            }

            if (controllerQuery.TryGetComponent(nodeOwner, out var controller))
            {
                LoadController(nodeOwner, controller);
                continue;
            }
        }

        Plasma.ConstrainedVolume = Torus.Count * LitersPerTorus;

        CoolantIn.Volume = Magnets.Count * LitersPerMagnet;
        CoolantOut.Volume = CoolantIn.Volume;

        return;

        void LoadTorus(EntityUid nodeOwner, FusionReactorTorusComponent torus, TransformComponent xform, MapGridComponent grid, EntityQuery<FusionReactorTorusComponent> torusQuery)
        {
            var mapSystem = _entityManager.System<MapSystem>();

            var nodeNeighbors = mapSystem.GetCellsInSquareArea(xform.GridUid!.Value, grid, xform.Coordinates, 1)
                .Where(entity => entity != nodeOwner && torusQuery.HasComponent(entity));

            if (nodeNeighbors.Count() >= 8)
            {
                Magnets.Add((nodeOwner, torus));
                _fusionReactorSystem?.SetMagnet(nodeOwner, torus, true);
            }
            else
            {
                Torus.Add((nodeOwner, torus));
                _fusionReactorSystem?.SetMagnet(nodeOwner, torus, false);
            }
        }

        void LoadController(EntityUid nodeOwner, FusionReactorControllerComponent controller)
        {
            if (MasterController == null)
                MasterController = (nodeOwner, controller);
        }

        void LoadBattery(EntityUid nodeOwner, FusionReactorBatteryComponent battery) =>
            Batteries.Add((nodeOwner, battery));

        void LoadMaser(EntityUid nodeOwner, FusionReactorMaserComponent maser) =>
            Masers.Add((nodeOwner, maser));
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        _fusionReactorSystem?.RemoveReactor(this);

        var newPlasma = new List<FusionMixture>(newGroups.Count());
        var newStored = new List<FusionMixture>(newGroups.Count());
        var newCoolantIn = new List<GasMixture>(newGroups.Count());
        var newCoolantOut = new List<GasMixture>(newGroups.Count());
        foreach (var group in newGroups)
        {
            if (group.Key is FusionReactorNodeGroup newGroup)
            {
                newPlasma.Add(newGroup.Plasma);
                newStored.Add(newGroup.Stored);
                newCoolantIn.Add(newGroup.CoolantIn);
                newCoolantOut.Add(newGroup.CoolantOut);

                newGroup.LastProcess = LastProcess;
                newGroup.RequestedMagneticPressure = RequestedMagneticPressure;

                newGroup.CanEject = CanEject;
                newGroup.HasDumpedCoolant = HasDumpedCoolant;

                newGroup.Integrity = Integrity;
                newGroup.IntegrityMax = IntegrityMax;
                newGroup.IntegrityMaxDecay = IntegrityMaxDecay;
                newGroup.IntegrityRegeneration = IntegrityRegeneration;

                newGroup.ResistancePressure = ResistancePressure;
                newGroup.ResistanceTemperature = ResistanceTemperature;

                newGroup.MeltdownAnnouncements = MeltdownAnnouncements;
                newGroup.MeltdownStage = MeltdownStage;

                newGroup.LastAnnouncedIntegrity = LastAnnouncedIntegrity;
                newGroup.AnnouncementInterval = AnnouncementInterval;

                newGroup.NextAllowedAnnouncement = NextAllowedAnnouncement;
                newGroup.NextEventTime = NextEventTime;
            }
        }

        _fusionSystem?.DivideInto(Plasma, newPlasma);
        _fusionSystem?.DivideInto(Stored, newStored);
        _atmosphereSystem?.DivideInto(CoolantIn, newCoolantIn);
        _atmosphereSystem?.DivideInto(CoolantOut, newCoolantOut);
    }

    private float StabilityCalculation()
    {
        var ex = Plasma.Volume / Plasma.ConstrainedVolume;
        var x = 1 - (0.5f * (float)ex);

        // Go put it in desmos yourself if you're so curious
        var value = Math.Clamp(1.5625f * (x - MathF.Pow(x, 8)), 0, 1);
        return !float.IsNaN(value) ? value : 0;
    }
}