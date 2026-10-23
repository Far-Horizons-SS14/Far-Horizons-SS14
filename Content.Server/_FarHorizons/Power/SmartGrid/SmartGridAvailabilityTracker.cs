using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
/// Shortage and recovery timers for one automatic availability signal.
/// </summary>
/// <remarks>
/// Not directly a component itself because we instantiate copies of this for both networks and some sources, and networks aren't entities.
/// </remarks>
internal sealed class SmartGridAvailabilityTracker
{
    /// <summary>
    ///     The availability calculated from this tracker's immediate sources, before upstream propagation.
    /// </summary>
    public SmartGridPriority LocalAvailability = SmartGridPriority.General;

    public bool SourceConstrained;
    public TimeSpan? ShortageSince;
    public TimeSpan? RecoverySince;
    public TimeSpan? RecoverySpoolUpUntil;

    /// <summary>
    ///     Sets the local availability and clears all pending shortage, recovery, and spool-up timers.
    /// </summary>
    public void Reset(SmartGridPriority localAvailability)
    {
        LocalAvailability = localAvailability;
        ShortageSince = null;
        RecoverySince = null;
        RecoverySpoolUpUntil = null;
    }

    /// <summary>
    ///     Advances the automatic availability by one evaluation.
    /// </summary>
    public void Update(
        TimeSpan now,
        bool shortage,
        bool recoveryReady,
        bool spoolUpAfterRecovery,
        TimeSpan shortageDuration,
        TimeSpan recoveryDuration,
        TimeSpan recoverySpoolUpDuration)
    {
        SourceConstrained = shortage;
        if (RecoverySpoolUpUntil > now && !shortage)
        {
            ShortageSince = null;
            RecoverySince = null;
            return;
        }

        RecoverySpoolUpUntil = null;
        if (shortage)
        {
            RecoverySince = null;
            ShortageSince ??= now;
            if (now - ShortageSince >= shortageDuration
                && LocalAvailability > SmartGridPriority.EssentialSystems)
            {
                LocalAvailability--;
                ShortageSince = now;
            }

            return;
        }

        ShortageSince = null;
        if (!recoveryReady || LocalAvailability == SmartGridPriority.General)
        {
            RecoverySince = null;
            return;
        }

        RecoverySince ??= now;
        if (now - RecoverySince < recoveryDuration)
            return;

        LocalAvailability++;
        RecoverySince = null;
        if (spoolUpAfterRecovery)
            RecoverySpoolUpUntil = now + recoverySpoolUpDuration;
    }
}
