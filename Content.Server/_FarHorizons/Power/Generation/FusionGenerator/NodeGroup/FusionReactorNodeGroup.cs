using System.Linq;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared._FarHorizons.Fusion;
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
            }
        }

        _fusionSystem?.DivideInto(Plasma, newPlasma);
        _fusionSystem?.DivideInto(Stored, newStored);
        _atmosphereSystem?.DivideInto(CoolantIn, newCoolantIn);
        _atmosphereSystem?.DivideInto(CoolantOut, newCoolantOut);
    }
}