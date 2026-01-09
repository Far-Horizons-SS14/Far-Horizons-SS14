using Content.Shared._FarHorizons.Towing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Server.Physics;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Physics;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.Coordinates;

namespace Content.Server._FarHorizons.Towing;
public sealed partial class TowingSystem : EntitySystem
{    
    [Dependency] private readonly JointSystem _joint = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    private static readonly string _towingRope = "TowingRope";
    public override void Initialize()
    {
        SubscribeLocalEvent<TowingComponent, GetVerbsEvent<UtilityVerb>>(OnAddUtilityVerb);
        SubscribeLocalEvent<TiedComponent, GetVerbsEvent<AlternativeVerb>>(OnAddAlternativeVerb);
        SubscribeLocalEvent<TowingComponent, TieUpDoAfter>(OnTieUpDoAfter);
        SubscribeLocalEvent<TiedComponent, UnTieDoAfter>(OnUnTieDoAfter);
        base.Initialize();
    }

    public void OnAddUtilityVerb(EntityUid ent, TowingComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if(!args.CanAccess || !args.CanInteract || args.Hands == null) return;
        if(HasComp<TiedComponent>(args.Target)) return;
        if(TryComp<PhysicsComponent>(args.Target, out var physicsComponent) && physicsComponent.BodyType == BodyType.Static) return;
        var tieVerb = new UtilityVerb
        {
            Text = "Tie Rope",
            Act = () =>
            {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.TieUpTime, new TieUpDoAfter(), ent, target: args.Target)
                    {
                        BreakOnMove = true,
                    };
                    
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
            }
        };
        args.Verbs.Add(tieVerb);
    }

    public void OnTieUpDoAfter(Entity<TowingComponent> ent, ref TieUpDoAfter args)
    {
        if(args.Cancelled) return;
        var target = args.Target!.Value;

        if(ent.Comp.EntityA == null)
        {
            var jointComp = EnsureComp<JointComponent>(target);
            var visualComp = EnsureComp<JointVisualsComponent>(target);
            var tiedComp = EnsureComp<TiedComponent>(target);
            var joint = _joint.CreateDistanceJoint(args.User, target, anchorA: new Vector2(0f, 0.5f), anchorB: new Vector2(0f, 0.5F));
            joint.MaxLength = joint.Length + 0.2f;
            joint.Stiffness = 1f;
            joint.MinLength = 0.35f;
            
            tiedComp.AttachedTo = args.User;
            
            visualComp.Sprite = ent.Comp.RopeSprite;
            visualComp.OffsetA = new Vector2(0f, 0.5f);
            visualComp.OffsetB = new Vector2(0f, 0.5f);
            visualComp.Target = args.User;

            ent.Comp.EntityA = target;

            Dirty(ent.Owner, ent.Comp);
            Dirty(target, tiedComp);
            Dirty(target, visualComp);
            Dirty(target, jointComp);
        }
        else
        {
            var entA = ent.Comp.EntityA!.Value;
            _joint.ClearJoints(entA);
            var visualComp = Comp<JointVisualsComponent>(entA);
            var tiedComp = EnsureComp<TiedComponent>(target);
            var joint = _joint.CreateDistanceJoint(entA, target, anchorA: new Vector2(0f, 0.5f), anchorB: new Vector2(0f, 0.5F));
            joint.MaxLength = joint.Length + 0.2f;
            joint.Stiffness = 1f;
            joint.MinLength = 0.35f;
            
            tiedComp.AttachedTo = entA;

            visualComp.Target = target;

            if(TryComp<TiedComponent>(entA, out var tied))
            {
                tied.AttachedTo = target;
                Dirty(entA, tied);
            }
            
            Dirty(target, tiedComp);
            Dirty(target, visualComp);
            QueueDel(ent.Owner);
        }
    }

    public void OnAddAlternativeVerb(EntityUid ent, TiedComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if(!args.CanAccess || !args.CanInteract || args.Hands == null) return;
        if(!HasComp<TiedComponent>(args.Target)) return;
        var untieRopeVerb = new AlternativeVerb
        {
            Text = "Untie Rope",
            Act = () =>
            {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.UntieTime, new UnTieDoAfter(), args.Target, target: args.Target)
                    {
                        BreakOnMove = true,
                    };
                    
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
            }
        };
        args.Verbs.Add(untieRopeVerb);
    }

    public void OnUnTieDoAfter(Entity<TiedComponent> ent, ref UnTieDoAfter args)
    {
        if(args.Cancelled) return;
        
        var target = args.Target!.Value;
        Logger.Info($"Target: {target}");
        if(TryComp<TiedComponent>(target, out var tComp))
        {
            var attachedTo = tComp.AttachedTo!.Value;
            Logger.Info($"attach: {attachedTo}");
            RemComp<TiedComponent>(attachedTo);
            RemComp<JointComponent>(attachedTo);
            RemComp<JointVisualsComponent>(attachedTo);
        }
        RemComp<TiedComponent>(target);
        RemComp<JointComponent>(target);
        RemComp<JointVisualsComponent>(target);
        _joint.RecursiveClearJoints(target);
        var newrope = SpawnAtPosition(_towingRope, target.ToCoordinates());
        _hands.TryPickupAnyHand(args.User, newrope);
    }
}