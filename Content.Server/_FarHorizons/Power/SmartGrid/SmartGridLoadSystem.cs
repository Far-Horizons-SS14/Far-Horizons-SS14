using Content.Server.Power.Components;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
///     Applies automatic smart-grid shedding without changing manual power state.
/// </summary>
public sealed partial class SmartGridLoadSystem : EntitySystem
{
    /// <summary>
    /// Enable smart grid participation for all ApcPowerReceivers by default
    /// </summary>
    [SubscribeLocalEvent]
    private void OnApcReceiverStartup(Entity<ApcPowerReceiverComponent> apcPowerReceiver, ref ComponentStartup args)
    {
        EnsureComp<SmartGridLoadComponent>(apcPowerReceiver.Owner);
    }

    /// <summary>
    ///     Enables or sheds a smart-grid load.
    /// </summary>
    /// <returns>
    ///     Whether the entity has a supported power load.
    /// </returns>
    public bool SetShed(EntityUid uid, bool shed, SmartGridLoadComponent? smartGridLoad = null)
    {
        if (!Resolve(uid, ref smartGridLoad, false))
            return false;

        if (smartGridLoad.Shed == shed)
            return true;

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
        {
            receiver.SmartGridDisabled = shed;
        }
        else if (TryComp<PowerConsumerComponent>(uid, out var consumer))
        {
            if (shed)
            {
                smartGridLoad.DirectConsumerEnabledBeforeShed = consumer.NetworkLoad.Enabled;
                consumer.NetworkLoad.Enabled = false;
            }
            else
            {
                consumer.NetworkLoad.Enabled = smartGridLoad.DirectConsumerEnabledBeforeShed ?? true;
                smartGridLoad.DirectConsumerEnabledBeforeShed = null;
            }
        }
        else
        {
            Log.Error($"Smart-grid load {ToPrettyString(uid)} has no supported power component.");
            return false;
        }

        smartGridLoad.Shed = shed;
        return true;
    }
}
