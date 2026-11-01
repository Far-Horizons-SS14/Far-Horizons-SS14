using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.PowerArmor;

public abstract partial class SharedPowerArmorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    protected virtual void OnPartInserted(Entity<PowerArmorPartComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        ent.Comp.AttachedTo = args.Container.Owner;
        if (!TryComp<PowerArmorComponent>(args.Container.Owner, out var paComp)) return;

        paComp.Parts[ent.Comp.PartType] = ent.Owner;
        RefreshArmorSpeed((args.Container.Owner, paComp));
    }

    [SubscribeLocalEvent]
    protected virtual void OnPartEjected(Entity<PowerArmorPartComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<PowerArmorComponent>(ent.Comp.AttachedTo, out var paComp)) return;

        paComp.Parts[ent.Comp.PartType] = null;
        ent.Comp.AttachedTo = null;
        RefreshArmorSpeed((args.Container.Owner, paComp));
    }

    [SubscribeLocalEvent]
    public void OnPartBroken(Entity<PowerArmorPartComponent> ent, ref BreakageEventArgs args)
    {
        if (ent.Comp.isBroken)
            return;

        ent.Comp.isBroken = true;
        Dirty(ent);

        if (!_container.TryGetContainingContainer(ent.Owner, out var container)
            || !TryComp<PowerArmorComponent>(container.Owner, out var paComp))
            return;

        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.PowerArmor, GetNetEntity(container.Owner));
        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.Visible, false);

        RefreshArmorSpeed((container.Owner, paComp));
    }

    [SubscribeLocalEvent]
    private void OnRepair(Entity<PowerArmorPartComponent> ent, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled || !ent.Comp.isBroken)
            return;

        ent.Comp.isBroken = false; 
        Dirty(ent);

        if (!_container.TryGetContainingContainer(ent.Owner, out var container)
            || !TryComp<PowerArmorComponent>(container.Owner, out var paComp))
            return;

        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.PowerArmor, GetNetEntity(container.Owner));
        _appearance.SetData(ent.Owner, PowerArmorPartVisuals.Visible, true);

        RefreshArmorSpeed((container.Owner, paComp));
    }

    [SubscribeLocalEvent]
    private void OnSlowdownQuery(Entity<PowerArmorPartComponent> ent, ref PowerArmorRelayedEvent<PowerArmorSlowdownQueryEvent> args)
    {
        if (ent.Comp.isBroken)
            return;

        args.Args.TotalSlowdown -= ent.Comp.SpeedModifier;
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PowerArmorPartComponent> ent, ref AfterInteractEvent args)
    {
        if(!TryComp<PowerArmorComponent>(args.Target, out var PAComp)
        || !PAComp.IsPrimary) 
            return;

        if(!_access.IsAllowed(args.User, ent.Owner))
        {
            _popUp.PopupEntity("Unauthorized Access Detected.", ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        foreach(var part in PAComp.Parts)
        {
            if(ent.Comp.PartType == part.Key && part.Value != null)
            {
                _popUp.PopupClient("This piece is already installed.", args.User);
                return;
            }
        }

        if(Exists(PAComp.OtherHalf))
        {
            if(TryComp<PowerArmorComponent>(PAComp.OtherHalf, out var PAComp2))
            {
                foreach(var part in PAComp2.Parts)
                {
                    if(ent.Comp.PartType == part.Key && part.Value != null)
                    {
                        _popUp.PopupClient("This piece is already installed.", args.User);
                        return;
                    }
                }
            }
        }

        var InstallDoAfter = new InstallPartDoAfter(ent.Comp.PartType , GetNetEntity(args.Used));
        var installDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallTime, InstallDoAfter, args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true
        };
        _doAfter.TryStartDoAfter(installDoAfter);
        args.Handled = true;   
    }

    [SubscribeLocalEvent]
    private void InRangeOverride(Entity<PowerArmorPartComponent> ent, ref InRangeOverrideEvent args)
    {
        if(!_container.TryGetOuterContainer(args.Target, Transform(args.Target), out var container))
            return;
        args.InRange = _interaction.InRangeUnobstructed(args.User, container.Owner, args.Range, args.CollisionMask, args.Predicate, args.Popup, args.OverlapCheck);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void AccessibleOverride(Entity<PowerArmorPartComponent> ent, ref AccessibleOverrideEvent args)
    {
        if(!_container.TryGetOuterContainer(args.Target, Transform(args.Target), out var container))
            return;
        args.Accessible = _interaction.IsAccessible(args.User, container.Owner);
        args.Handled = true;
    }

    private void RefreshArmorSpeed(Entity<PowerArmorComponent> armor)
    {
        var ev = new PowerArmorSlowdownQueryEvent();
        RaiseLocalEvent(armor.Owner, ref ev);
        armor.Comp.TotalSpeedModifier = ev.TotalSlowdown;
        Dirty(armor);

        if (armor.Comp.Wearer != null)
            _movement.RefreshMovementSpeedModifiers(armor.Comp.Wearer.Value);
    }
}