using Content.Shared.Interaction;

namespace Content.Shared._FarHorizons.PowerArmor;

public abstract partial class SharedPowerArmorSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<PowerArmorComponent, InteractUsingEvent>(RefRelayPAPartsEvent);
        SubscribeLocalEvent<PowerArmorComponent, PowerArmorSlowdownQueryEvent>(RefRelayPAPartsEvent);
    }

    protected void RefRelayPAPartsEvent<T>(EntityUid uid, PowerArmorComponent component, ref T args) where T : IPowerArmorRelayedEvent
        => RelayEvent((uid, component), ref args);

    public void RelayEvent<T>(Entity<PowerArmorComponent> powerArmor, ref T args) where T : IPowerArmorRelayedEvent
    {
        var ev = new PowerArmorRelayedEvent<T>(args, powerArmor.Owner);
        foreach (var part in powerArmor.Comp.Parts)
        {
            if(part.Value == null) continue;
            RaiseLocalEvent(part.Value.Value, ev);
        }

        if(TryComp<PowerArmorComponent>(powerArmor.Comp.OtherHalf, out var hPAComp))
            foreach (var part in hPAComp.Parts)
            {
                if(part.Value == null) continue;

                RaiseLocalEvent(part.Value.Value, ev);
            }

        args = ev.Args;
    }
}