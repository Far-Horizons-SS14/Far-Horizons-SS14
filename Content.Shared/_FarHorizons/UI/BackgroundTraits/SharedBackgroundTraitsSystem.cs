using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.UI.BackgroundTraits;

public abstract partial class SharedBackgroundTraitSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedActionsSystem _actions = default!;
    [Dependency] protected IComponentFactory _compFactory = default!;
    [Dependency] protected ILogManager _log = default!;
    [Dependency] protected SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
        => base.Initialize();

    public List<ProtoId<BackgroundTraitPrototype>> ValidatedTraits(List<ProtoId<BackgroundTraitPrototype>> traits)
    {
        var result = new List<ProtoId<BackgroundTraitPrototype>>();
        var points = 0;

        foreach (var t in traits)
        {
            var proto = ProtoMan.Index(t);
            if (proto.Incompatible.Intersect(result).Any()) continue;

            points -= proto.Cost;
            result.Add(t);
        }

        return points >= 0 ? result : [];
    }
}

public abstract class BackgroundTraitSystem<TBase, T> : EntitySystem
    where TBase : Component
    where T : Component
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<T> ent, ref MapInitEvent args)
    {
        if (!TryComp<TBase>(ent, out var anchor)) return;
        TraitInit((ent.Owner, anchor, ent.Comp));
    }

    protected virtual void TraitInit(Entity<TBase, T> ent) { }
}

public abstract partial class BackgroundPassiveTraitSystem<TBase, T> : BackgroundTraitSystem<TBase, T>
    where TBase : Component
    where T : BackgroundPassiveTraitComponent
{
    [Dependency] protected IGameTiming Timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<TBase, T>();
        while (query.MoveNext(out var uid, out var anchor, out var comp))
        {
            if (comp.TickRate == TimeSpan.Zero || Timing.CurTime < comp.NextUpdate) continue;
            comp.NextUpdate = Timing.CurTime + comp.TickRate;
            UpdateEffect((uid, anchor, comp));
        }
    }

    protected virtual void UpdateEffect(Entity<TBase, T> ent) { }
}

public abstract partial class BackgroundActionTraitSystem<TBase, T, TEvent> : BackgroundTraitSystem<TBase, T>
    where TBase : Component
    where T : BackgroundActionTraitComponent
    where TEvent : BaseActionEvent
{
    [Dependency] protected SharedActionsSystem Actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, TEvent>(OnActionUsed);
    }

    private void OnActionUsed(Entity<T> ent, ref TEvent args)
    {
        if (!TryComp<TBase>(ent, out var anchor)) return;
        ActionUsed((ent.Owner, anchor, ent.Comp), ref args);
        args.Handled = true;
    }

    protected override void TraitInit(Entity<TBase, T> ent)
    {
        base.TraitInit(ent);
        Actions.AddAction(ent, ent.Comp2.Action);
    }

    protected virtual void ActionUsed(Entity<TBase, T> ent, ref TEvent args) { }
}

public abstract partial class BackgroundToggleActionTraitSystem<TBase, T, TEvent> : BackgroundActionTraitSystem<TBase, T, TEvent>
    where TBase : Component
    where T : BackgroundToggleActionComponent
    where TEvent : InstantActionEvent
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize() 
    {
        base.Initialize();
        SubscribeLocalEvent<T, MobStateChangedEvent>(OnDeath);
    }

    protected override void TraitInit(Entity<TBase, T> ent)
    {
        base.TraitInit(ent);

        var action = Actions.GetActions(ent)
            .Where(p => MetaData(p).EntityPrototype is { } entProto && entProto.ID == ent.Comp2.Action)
            .FirstOrNull();

        if (action == null) return;

        Actions.SetToggled(action.Value.AsNullable(), ent.Comp2.Toggled);
    }

    protected override void ActionUsed(Entity<TBase, T> ent, ref TEvent args)
    {
        base.ActionUsed(ent, ref args);

        if (_net.IsServer)
        {
            ent.Comp2.Toggled = !ent.Comp2.Toggled;
            Dirty(ent);
        }

        Actions.SetToggled(args.Action.AsNullable(), ent.Comp2.Toggled);
        OnToggled(ent, ent.Comp2.Toggled);
        args.Handled = true;
    }

    protected virtual void OnToggled(Entity<TBase, T> ent, bool toggle) { }

    protected virtual void OnDeath(Entity<T> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<TBase>(ent, out var anchor)) return;
        if (_net.IsServer && args.NewMobState.Equals(MobState.Dead))
        {
            ent.Comp.Toggled = false;
            Dirty(ent);
        }

        OnToggled((ent.Owner, anchor, ent.Comp), ent.Comp.Toggled);
        var action = Actions.GetActions(ent)
            .Where(p => MetaData(p).EntityPrototype is { } entProto && entProto.ID == ent.Comp.Action)
            .FirstOrNull();

        if (action == null) return;

        Actions.SetToggled(action.Value.AsNullable(), ent.Comp.Toggled);
    }
}
public abstract class BackgroundTraitSystem<T> : BackgroundTraitSystem<BackgroundTraitComponent, T>
    where T : BackgroundTraitComponent { }

public abstract class BackgroundPassiveTraitSystem<T> : BackgroundPassiveTraitSystem<BackgroundTraitComponent, T>
    where T : BackgroundPassiveTraitComponent { }

public abstract class BackgroundActionTraitSystem<T, TEvent> : BackgroundActionTraitSystem<BackgroundTraitComponent, T, TEvent>
    where T : BackgroundActionTraitComponent
    where TEvent : BaseActionEvent { }

public abstract class BackgroundToggleActionTraitSystem<T, TEvent> : BackgroundToggleActionTraitSystem<BackgroundTraitComponent, T, TEvent>
    where T : BackgroundToggleActionComponent
    where TEvent : InstantActionEvent { }