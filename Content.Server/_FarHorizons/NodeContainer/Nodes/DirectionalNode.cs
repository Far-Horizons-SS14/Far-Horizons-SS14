using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.NodeContainer.Nodes;

/// <summary>
///     Connects with other <see cref="DirectionalNode"/>s whose <see cref="NodeDirection"/>
///     correctly correspond.
/// </summary>
/// <remarks>
///     A copy-paste of <see cref="PipeNode"/> that is more generalized.
/// </remarks>
[DataDefinition]
[Virtual]
public partial class DirectionalNode : Node, IRotatableNode
{
    /// <summary>
    ///     The directions in which this node can connect to other nodes around it.
    /// </summary>
    [DataField("nodeDirection")]
    public NodeDirection OriginalNodeDirection;

    /// <summary>
    ///     The key that the node will try to connect with.
    /// </summary>
    [DataField("key")]
    public string? GroupKey { get; private set; }

    /// <summary>
    ///     The *current* node directions (accounting for rotation)
    ///     Used to check if this node can connect to another node in a given direction.
    /// </summary>
    public NodeDirection CurrentNodeDirection { get; private set; }

    private HashSet<DirectionalNode>? _alwaysReachable;

    public void AddAlwaysReachable(DirectionalNode directionalNode)
    {
        if (directionalNode.NodeGroupID != NodeGroupID) return;
        _alwaysReachable ??= [];
        _alwaysReachable.Add(directionalNode);

        if (NodeGroup != null)
            IoCManager.Resolve<IEntityManager>().System<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup)NodeGroup);
    }

    public void RemoveAlwaysReachable(DirectionalNode directionalNode)
    {
        if (_alwaysReachable == null) return;

        _alwaysReachable.Remove(directionalNode);

        if (NodeGroup != null)
            IoCManager.Resolve<IEntityManager>().System<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup)NodeGroup);
    }

    /// <summary>
    ///     Whether this node can connect to others or not.
    /// </summary>
    [DataField("connectionsEnabled")]
    public bool ConnectionsEnabled
    {
        get;
        set
        {
            field = value;

            if (NodeGroup != null)
                IoCManager.Resolve<IEntityManager>().System<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup)NodeGroup);
        }
    } = true;

    public override bool Connectable(IEntityManager entMan, TransformComponent? xform = null) => ConnectionsEnabled && base.Connectable(entMan, xform);

    [DataField("rotationsEnabled")]
    public bool RotationsEnabled { get; set; } = true;

    public override void Initialize(EntityUid owner, IEntityManager entMan)
    {
        base.Initialize(owner, entMan);

        if (!RotationsEnabled)
            return;

        var xform = entMan.GetComponent<TransformComponent>(owner);
        CurrentNodeDirection = OriginalNodeDirection.RotateNodeDirection(xform.LocalRotation);
    }

    bool IRotatableNode.RotateNode(in MoveEvent ev)
    {
        if (OriginalNodeDirection == NodeDirection.Fourway)
            return false;

        // update valid node direction
        if (!RotationsEnabled)
        {
            if (CurrentNodeDirection == OriginalNodeDirection)
                return false;

            CurrentNodeDirection = OriginalNodeDirection;
            return true;
        }

        var oldDirection = CurrentNodeDirection;
        CurrentNodeDirection = OriginalNodeDirection.RotateNodeDirection(ev.NewRotation);
        return oldDirection != CurrentNodeDirection;
    }

    public override void OnAnchorStateChanged(IEntityManager entityManager, bool anchored)
    {
        if (!anchored)
            return;

        // update valid node directions

        if (!RotationsEnabled)
        {
            CurrentNodeDirection = OriginalNodeDirection;
            return;
        }

        var xform = entityManager.GetComponent<TransformComponent>(Owner);
        CurrentNodeDirection = OriginalNodeDirection.RotateNodeDirection(xform.LocalRotation);
    }

    public override IEnumerable<Node> GetReachableNodes(
        Entity<TransformComponent> xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        Entity<MapGridComponent>? grid,
        IEntityManager entMan)
    {
        if (_alwaysReachable != null)
        {
            var remQ = new RemQueue<DirectionalNode>();
            foreach (var node in _alwaysReachable)
            {
                if (node.Deleting)
                {
                    remQ.Add(node);
                }
                yield return node;
            }

            foreach (var node in remQ)
            {
                _alwaysReachable.Remove(node);
            }
        }

        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var mapSystem = entMan.System<SharedMapSystem>();
        var pos = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        for (var i = 0; i < NodeDirectionHelpers.NodeDirections; i++)
        {
            var nodeDir = (NodeDirection)(1 << i);

            if (!CurrentNodeDirection.HasDirection(nodeDir))
                continue;

            foreach (var node in LinkableNodesInDirection(pos, nodeDir, gridEnt, nodeQuery, mapSystem))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    ///     Gets the nodes that can connect to us from entities on the tile or adjacent in a direction.
    /// </summary>
    private IEnumerable<DirectionalNode> LinkableNodesInDirection(
        Vector2i pos,
        NodeDirection nodeDir,
        Entity<MapGridComponent> grid,
        EntityQuery<NodeContainerComponent> nodeQuery,
        SharedMapSystem mapSystem)
    {
        foreach (var node in NodesInDirection(pos, nodeDir, grid, nodeQuery, mapSystem))
        {
            if (node.NodeGroupID == NodeGroupID
                && node.GroupKey == GroupKey
                && node.CurrentNodeDirection.HasDirection(nodeDir.GetOpposite()))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    ///     Gets the nodes from entities on the tile adjacent in a direction.
    /// </summary>
    protected IEnumerable<DirectionalNode> NodesInDirection(
        Vector2i pos,
        NodeDirection nodeDir,
        Entity<MapGridComponent> grid,
        EntityQuery<NodeContainerComponent> nodeQuery,
        SharedMapSystem mapSystem)
    {
        var offsetPos = pos.Offset(nodeDir.ToDirection());

        foreach (var entity in mapSystem.GetAnchoredEntities(grid, offsetPos))
        {
            if (!nodeQuery.TryGetComponent(entity, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                if (node is DirectionalNode dirNode)
                    yield return dirNode;
            }
        }
    }
}

public enum NodeDirection
{
    None = 0,

    North = 1 << 0,
    South = 1 << 1,
    West = 1 << 2,
    East = 1 << 3,

    Fourway = North | South | East | West,

    All = -1,
}

public static class NodeDirectionHelpers
{
    public const int NodeDirections = 4;

    public static bool HasDirection(this NodeDirection nodeDirection, NodeDirection other) => (nodeDirection & other) == other;

    public static Angle ToAngle(this NodeDirection nodeDirection) => nodeDirection.ToDirection().ToAngle();

    public static NodeDirection ToNodeDirection(this Direction direction) => direction switch
    {
        Direction.North => NodeDirection.North,
        Direction.South => NodeDirection.South,
        Direction.East => NodeDirection.East,
        Direction.West => NodeDirection.West,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public static Direction ToDirection(this NodeDirection nodeDirection) => nodeDirection switch
    {
        NodeDirection.North => Direction.North,
        NodeDirection.South => Direction.South,
        NodeDirection.East => Direction.East,
        NodeDirection.West => Direction.West,
        _ => throw new ArgumentOutOfRangeException(nameof(nodeDirection)),
    };

    public static NodeDirection GetOpposite(this NodeDirection nodeDirection) => nodeDirection switch
    {
        NodeDirection.North => NodeDirection.South,
        NodeDirection.South => NodeDirection.North,
        NodeDirection.East => NodeDirection.West,
        NodeDirection.West => NodeDirection.East,
        _ => throw new ArgumentOutOfRangeException(nameof(nodeDirection)),
    };

    public static NodeDirection RotateNodeDirection(this NodeDirection nodeDirection, double diff)
    {
        var newNodeDir = NodeDirection.None;
        for (var i = 0; i < NodeDirections; i++)
        {
            var currentNodeDirection = (NodeDirection)(1 << i);
            if (!nodeDirection.HasFlag(currentNodeDirection)) continue;
            var angle = currentNodeDirection.ToAngle();
            angle += diff;
            newNodeDir |= angle.GetCardinalDir().ToNodeDirection();
        }
        return newNodeDir;
    }
}
