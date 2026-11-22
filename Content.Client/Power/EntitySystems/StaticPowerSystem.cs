using Content.Client.Power.Components;
using Content.Shared._FarHorizons.Power.Components;

namespace Content.Client.Power.EntitySystems;

public static class StaticPowerSystem
{
    // Using this makes the call shorter.
    // ReSharper disable once UnusedParameter.Global
    public static bool IsPowered(this EntitySystem system, EntityUid uid, IEntityManager entManager, ApcPowerReceiverComponent? receiver = null)
    {
        // Far Horizons start - support power-wire on non-anchored, non-battery entities like lockers and simple bots
        if (entManager.TryGetComponent(uid, out PassivePowerComponent? passivePower))
            return passivePower.Powered;
        // Far Horizons end

        if (receiver == null && !entManager.TryGetComponent(uid, out receiver))
            return true;

        return receiver.Powered;
    }
}
