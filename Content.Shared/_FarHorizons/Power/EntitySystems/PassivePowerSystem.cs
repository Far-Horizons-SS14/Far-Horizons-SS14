using Content.Shared._FarHorizons.Power.Components;
using Content.Shared.Power;
using Robust.Shared.GameObjects;

namespace Content.Shared._FarHorizons.Power.EntitySystems;

public sealed partial class PassivePowerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public void SetPowered(EntityUid uid, bool powered, PassivePowerComponent? power = null)
    {
        if (!Resolve(uid, ref power) || power.Powered == powered)
            return;

        _appearance.SetData(uid, PowerDeviceVisuals.Powered, powered);

        power.Powered = powered;
        Dirty(uid, power);

        var ev = new PowerChangedEvent(powered, 0);
        RaiseLocalEvent(uid, ref ev);
    }

    public void TogglePower(EntityUid uid, PassivePowerComponent? power = null, bool? powered = null)
    {
        if (!Resolve(uid, ref power))
            return;

        SetPowered(uid, powered ?? !power.Powered, power);
    }
}
