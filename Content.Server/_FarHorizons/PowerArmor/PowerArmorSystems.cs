
using Content.Server.Destructible;
using Content.Shared._FarHorizons.PowerArmor;
using Robust.Shared.Containers;

namespace Content.Server._FarHorizons.PowerArmor;

public sealed partial class PowerArmorSystem : SharedPowerArmorSystem
{
    [Dependency] DestructibleSystem _destructible = default!;   
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerArmorPartComponent, MapInitEvent>(OnPAPInit);
        SubscribeLocalEvent<PowerArmorComponent, EntGotInsertedIntoContainerMessage>(OnPAInsert);
    }

    private void OnPAPInit(Entity<PowerArmorPartComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxIntegrity = _destructible.DestroyedAt(ent.Owner);
        Dirty(ent);
    }

    private void OnPAInsert(Entity<PowerArmorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var PowerArmor = args.Container.Owner;
        if(!TryComp<PowerArmorComponent>(PowerArmor, out var PAComp))
            return;

        ent.Comp.OtherHalf = PowerArmor;
        PAComp.OtherHalf = ent.Owner;

        ent.Comp.IsPrimary = false;
        PAComp.IsPrimary = true;

        Dirty(PowerArmor, PAComp);
        Dirty(ent);
    }

    protected override void OnPartInserted(Entity<PowerArmorPartComponent> ent, ref EntGotInsertedIntoContainerMessage args) 
        => base.OnPartInserted(ent, ref args);

    protected override void OnPartEjected(Entity<PowerArmorPartComponent> ent, ref EntGotRemovedFromContainerMessage args) 
        => base.OnPartEjected(ent, ref args);
}