using Content.Server.Power.NodeGroups;
using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
///     Boundary role used to apply the correct output-signal aggregation rule.
/// </summary>
internal enum SmartGridBoundaryKind : byte
{
    Battery,
    Generator,
}

/// <summary>
///     A device that can emit an availability signal to an output network.
/// </summary>
internal readonly record struct SmartGridBoundary(
    EntityUid Owner,
    IBasePowerNet? Input,
    IBasePowerNet Output,
    SmartGridBoundaryKind Kind);

/// <summary>
///     The immutable topology and mutable work lists for one smart-grid evaluation.
/// </summary>
internal sealed class SmartGridEvaluationSnapshot
{
    public HashSet<IBasePowerNet> Networks { get; } = new();
    public Dictionary<IBasePowerNet, List<EntityUid>> LoadsByNetwork { get; } = new();
    public List<SmartGridBoundary> Boundaries { get; } = new();
    public Dictionary<IBasePowerNet, SmartGridPriority> ManualOutputs { get; } = new();
    public HashSet<EntityUid> Generators { get; } = new();
    public Dictionary<EntityUid, IBasePowerNet> PrimaryBatteries { get; } = new();

    public void AddLoad(IBasePowerNet network, EntityUid load)
    {
        Networks.Add(network);
        if (!LoadsByNetwork.TryGetValue(network, out var loads))
        {
            loads = new();
            LoadsByNetwork.Add(network, loads);
        }

        loads.Add(load);
    }

    public void AddManualOutput(IBasePowerNet output, SmartGridPriority availability)
    {
        ManualOutputs[output] = ManualOutputs.TryGetValue(output, out var existing)
            ? Minimum(existing, availability)
            : availability;
    }

    private static SmartGridPriority Minimum(SmartGridPriority first, SmartGridPriority second)
    {
        return (SmartGridPriority) Math.Min((byte) first, (byte) second);
    }
}

/// <summary>
///     Aggregates all signals sent to one output network. Parallel batteries and generators each use their strongest
///     signal, while the network's own local availability remains a final ceiling.
/// </summary>
internal sealed class SmartGridOutputSignalAggregation
{
    private SmartGridPriority? _batteryAvailability;
    private SmartGridPriority? _generatorAvailability;

    public void Add(SmartGridBoundaryKind kind, SmartGridPriority signal)
    {
        switch (kind)
        {
            case SmartGridBoundaryKind.Battery:
                    _batteryAvailability = _batteryAvailability is { } existingBattery
                        ? Maximum(existingBattery, signal)
                        : signal;
                    break;

            case SmartGridBoundaryKind.Generator:
                    _generatorAvailability = _generatorAvailability is { } existingGenerator
                    ? Maximum(existingGenerator, signal)
                    : signal;
                    break;
        }
    }

    public SmartGridPriority ApplyTo(SmartGridPriority localAvailability)
    {
        var effectiveAvailability = localAvailability;
        if (_batteryAvailability is { } batteryAvailability)
            effectiveAvailability = Minimum(effectiveAvailability, batteryAvailability);
        if (_generatorAvailability is { } generatorAvailability)
            effectiveAvailability = Minimum(effectiveAvailability, generatorAvailability);
        return effectiveAvailability;
    }

    private static SmartGridPriority Minimum(SmartGridPriority first, SmartGridPriority second)
    {
        return (SmartGridPriority) Math.Min((byte) first, (byte) second);
    }

    private static SmartGridPriority Maximum(SmartGridPriority first, SmartGridPriority second)
    {
        return (SmartGridPriority) Math.Max((byte) first, (byte) second);
    }
}
