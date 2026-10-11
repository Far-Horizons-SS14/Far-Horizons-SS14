using System.Linq;
using Content.Shared.Actions;
using Content.Shared._FarHorizons.UI.BackgroundTraits;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Content.Shared.Mobs;

namespace Content.Shared._FarHorizons.Vampire.Traits;

public abstract partial class LesserVampireTraitSystem<T>
    : BackgroundTraitSystem<LesserVampireComponent, T>
    where T : LesserVampireTraitComponent
{
    [Dependency] protected SharedLesserVampireSystem Vampire = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, GetVampireBloodPoolChange>(OnBloodPoolChange);
    }

    private void OnBloodPoolChange(Entity<T> ent, ref GetVampireBloodPoolChange args)
    {
        if (!TryComp<LesserVampireComponent>(ent, out var vampire)) return;
        args.Change -= ent.Comp.PassiveDrain;
        RefreshBloodpoolDrain((ent.Owner, vampire, ent.Comp), ref args);
    }

    protected override void TraitInit(Entity<LesserVampireComponent, T> ent)
    {
        base.TraitInit(ent);
        Vampire.RefreshBloodPoolChange((ent.Owner, ent.Comp1));
    }

    protected virtual void RefreshBloodpoolDrain(Entity<LesserVampireComponent, T> ent, ref GetVampireBloodPoolChange args) { }
}

public abstract partial class LesserVampirePassiveTraitSystem<T>
    : BackgroundPassiveTraitSystem<LesserVampireComponent, T>
    where T : LesserVampirePassiveTraitComponent
{
    [Dependency] protected SharedLesserVampireSystem Vampire = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, GetVampireBloodPoolChange>(OnBloodPoolChange);
    }

    private void OnBloodPoolChange(Entity<T> ent, ref GetVampireBloodPoolChange args)
    {
        if (!TryComp<LesserVampireComponent>(ent, out var vampire)) return;
        args.Change -= ent.Comp.PassiveDrain;
        RefreshBloodpoolDrain((ent.Owner, vampire, ent.Comp), ref args);
    }

    protected override void TraitInit(Entity<LesserVampireComponent, T> ent)
    {
        base.TraitInit(ent);
        Vampire.RefreshBloodPoolChange((ent.Owner, ent.Comp1));
    }

    protected virtual void RefreshBloodpoolDrain(Entity<LesserVampireComponent, T> ent, ref GetVampireBloodPoolChange args) { }
}

public abstract partial class LesserVampireActionTraitSystem<T, TEvent>
    : BackgroundActionTraitSystem<LesserVampireComponent, T, TEvent>
    where T : LesserVampireActionTraitComponent
    where TEvent : BaseActionEvent
{
    [Dependency] protected SharedLesserVampireSystem Vampire = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, GetVampireBloodPoolChange>(OnBloodPoolChange);
    }

    private void OnBloodPoolChange(Entity<T> ent, ref GetVampireBloodPoolChange args)
    {
        if (!TryComp<LesserVampireComponent>(ent, out var vampire)) return;
        args.Change -= ent.Comp.PassiveDrain;
        RefreshBloodpoolDrain((ent.Owner, vampire, ent.Comp), ref args);
    }

    protected override void TraitInit(Entity<LesserVampireComponent, T> ent)
    {
        base.TraitInit(ent);
        Vampire.RefreshBloodPoolChange((ent.Owner, ent.Comp1));
    }

    protected virtual void RefreshBloodpoolDrain(Entity<LesserVampireComponent, T> ent, ref GetVampireBloodPoolChange args) { }
}

public abstract partial class LesserVampireToggleActionTraitSystem<T, TEvent>
    : BackgroundToggleActionTraitSystem<LesserVampireComponent, T, TEvent>
    where T : LesserVampireToggleActionComponent
    where TEvent : InstantActionEvent
{
    [Dependency] protected SharedLesserVampireSystem Vampire = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, GetVampireBloodPoolChange>(OnBloodPoolChange);
        SubscribeLocalEvent<T, OutOfBloodPoolEvent>(OnOutOfBloodPool);
    }

    private void OnBloodPoolChange(Entity<T> ent, ref GetVampireBloodPoolChange args)
    {
        if (!TryComp<LesserVampireComponent>(ent, out var vampire)) return;
        args.Change -= ent.Comp.PassiveDrain;
        if (ent.Comp.Toggled)
            args.Change -= ent.Comp.DrainWhenToggled;
        RefreshBloodpoolDrain((ent.Owner, vampire, ent.Comp), ref args);
    }

    private void OnOutOfBloodPool(Entity<T> ent, ref OutOfBloodPoolEvent args)
    {
        if (!TryComp<LesserVampireComponent>(ent, out var vampire) || !ent.Comp.Toggled) return;

        ent.Comp.Toggled = false;
        Vampire.RefreshBloodPoolChange((ent, vampire));
        Dirty(ent);

        var action = Actions.GetActions(ent)
            .Where(p => MetaData(p).EntityPrototype is { } entProto && entProto.ID == ent.Comp.Action)
            .FirstOrNull();
        if (action == null) return;
        Actions.SetToggled(action.Value.AsNullable(), false);
    }

    protected override void ActionUsed(Entity<LesserVampireComponent, T> ent, ref TEvent args)
    {
        if (Vampire.GetBloodPool(ent) == 0) return;
        base.ActionUsed(ent, ref args);

        if (_net.IsServer)
            Vampire.RefreshBloodPoolChange((ent.Owner, ent.Comp1));
    }

    protected override void OnDeath(Entity<T> ent, ref MobStateChangedEvent args) => base.OnDeath(ent, ref args);

    protected virtual void RefreshBloodpoolDrain(Entity<LesserVampireComponent, T> ent, ref GetVampireBloodPoolChange args) { }
}