using Content.Server.Power.NodeGroups;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
/// Evaluates power-network availability and applies staged load shedding after the power solver updates.
/// </summary>
public sealed partial class SmartGridSystem : EntitySystem
{
    /// <summary>
    /// The "tick rate" of the smart grid evaluation.
    /// </summary>
    private static readonly TimeSpan EvaluationInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long a shortage must persist to trigger load shedding.
    /// </summary>
    private static readonly TimeSpan ShortageDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long a network must exceed the requirements of its loads to recover from load shedding.
    /// </summary>
    private static readonly TimeSpan RecoveryDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long of a grace period to allow after recovery to allow upstream generation to spool up before shedding again.
    /// </summary>
    private static readonly TimeSpan RecoverySpoolUpDuration = TimeSpan.FromSeconds(5);

    private static readonly SmartGridPriority MinimumAvailability = SmartGridPriority.EssentialSystems;
    private static readonly SmartGridPriority DefaultAvailability = SmartGridPriority.General;
    private static readonly SmartGridPriority MaximumAvailability = SmartGridPriority.General;

    private const float SourceSaturation = 0.95f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SmartGridLoadSystem _smartGridLoad = default!;

    // Network state combines local automatic availability with the signal effective after propagation.
    private readonly Dictionary<IBasePowerNet, SmartGridNetworkAvailabilityState> _networkStates = new();

    private TimeSpan _nextEvaluation;

    /// <summary>
    ///     Evaluates smart-grid boundaries after the power solver has updated battery state.
    /// </summary>
    public void UpdateAfterPowerSolver()
    {
        if (_timing.CurTime < _nextEvaluation)
            return;

        _nextEvaluation = _timing.CurTime + EvaluationInterval;

        var evaluation = BuildEvaluationSnapshot();
        RemoveInactiveStates(evaluation);
        UpdateAutomaticAvailability(evaluation);
        ApplyManualOverrides(evaluation);
        PropagateAvailability(evaluation.Boundaries, evaluation.ManualOutputs);
        ApplyLoadAvailability(evaluation.LoadsByNetwork);
    }
}
