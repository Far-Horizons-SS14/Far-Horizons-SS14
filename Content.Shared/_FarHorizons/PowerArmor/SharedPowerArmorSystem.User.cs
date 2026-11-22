using Content.Shared.Item;

namespace Content.Shared._FarHorizons.PowerArmor;

public abstract partial class SharedPowerArmorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGettingPickedUp(Entity<PowerArmorUserComponent> ent, ref GettingPickedUpAttemptEvent args)
        => args.Cancel();
}