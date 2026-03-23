using System.Linq;
using Content.Shared._FarHorizons.Weapons.Ranged.Components;
using Content.Shared._FarHorizons.Weapons.Ranged.Events;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared._FarHorizons.Weapons.Ranged.Systems;

public sealed partial class BallisticAmmoDistributorSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _signal = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private (float accumulator, float threshold) _updateTime = (0, 0.75f); // There is no reason this is like this other than I found it funny

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BallisticAmmoDistributorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BallisticAmmoReceiverComponent, MapInitEvent>(OnReceiverInit);

        SubscribeLocalEvent<BallisticAmmoDistributorComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeLocalEvent<BallisticAmmoDistributorComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<BallisticAmmoDistributorComponent, NewLinkEvent>(OnNewLink);

        SubscribeLocalEvent<BallisticAmmoDistributorComponent, AmmoDistributorUpdateEvent>(OnUpdate);
    }

    private void OnMapInit(EntityUid uid, BallisticAmmoDistributorComponent comp, ref MapInitEvent args)
    {
        _signal.EnsureSourcePorts(uid, comp.DistributorConnectionPort);

        comp.AmmoSource = EnsureComp<BallisticAmmoProviderComponent>(uid);

        CollectReceivers(uid, comp);
    }

    private void CollectReceivers(EntityUid uid, BallisticAmmoDistributorComponent comp)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        comp.Receivers.Clear();

        List<EntityUid> badConnections = [];
        foreach (var (turretUid, _) in source.LinkedPorts)
        {
            if (!TryComp<BallisticAmmoProviderComponent>(turretUid, out var ammoProvider))
            {
                badConnections.Add(turretUid);
                continue;
            }

            if (comp.AmmoSource.Whitelist == null)
                SharedGunSystem.UpdateWhitelist(comp.AmmoSource, ammoProvider.Whitelist);

            if (!_whitelistSystem.WhitelistCompare(comp.AmmoSource.Whitelist, ammoProvider.Whitelist))
            {
                badConnections.Add(turretUid);
                continue;
            }

            comp.Receivers.Add((turretUid, ammoProvider));
        }

        foreach (var connection in badConnections)
            _signal.RemoveSinkFromSource(uid, connection);
    }

    private void OnReceiverInit(EntityUid uid, BallisticAmmoReceiverComponent comp, ref MapInitEvent args) => _signal.EnsureSinkPorts(uid, comp.ReceiverConnectionPort);

    private void OnGetVerbs(EntityUid uid, BallisticAmmoDistributorComponent comp, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using != null && comp.AmmoSource != null && TryComp<BallisticAmmoProviderComponent>(args.Using, out var ammoProvider))
        {
            if (comp.AmmoSource.Whitelist != null && _whitelistSystem.WhitelistCompare(ammoProvider.Whitelist, comp.AmmoSource.Whitelist))
            {
                var itemUid = args.Using.Value;
                var userId = args.User;
                Verb transferVerb = new() /// I would make this an <see cref="InteractionVerb"/>, but it stops working if I do
                {
                    Text = Loc.GetString("ammo-distributor-transfer-verb"),
                    TextStyleClass = "InteractionVerb",
                    // This could be done with a do-after, but I'm too lazy for that
                    Act = () =>
                    {
                        var receiverLacking = _gunSystem.GetAmmoCapacity(uid) - _gunSystem.GetAmmoCount(uid);
                        if (receiverLacking <= 0)
                            return;

                        var ammoEv = new TakeAmmoEvent(Math.Min(_gunSystem.GetAmmoCount(itemUid), receiverLacking), [], Transform(uid).Coordinates, userId);
                        RaiseLocalEvent(itemUid, ammoEv);

                        if (ammoEv.Ammo.Count <= 0)
                            return;

                        foreach (var (bullet, _) in ammoEv.Ammo)
                        {
                            if (_gunSystem.IsFull((uid, comp.AmmoSource)))
                                break;

                            if (bullet == null)
                                continue;

                            _gunSystem.TryBallisticInsert((uid, comp.AmmoSource), bullet.Value, uid, true);
                        }
                    },
                    IconEntity = GetNetEntity(args.Using),
                };
                args.Verbs.Add(transferVerb);
            }
        }

        if (_gunSystem.GetAmmoCount(uid) <= 0)
        {
            /// I would make this an <see cref="ActivationVerb"/>, but it stops working if I do
            Verb cutVerb = new()
            {
                Text = Loc.GetString("ammo-distributor-cut-connections-verb"),
                TextStyleClass = "ActivationVerb",
                Act = () => ClearConnections(uid, comp)
            };
            args.Verbs.Add(cutVerb);
        }
    }

    private void ClearConnections(EntityUid uid, BallisticAmmoDistributorComponent comp)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        var connections = source.LinkedPorts.Select(p => p.Key);
        foreach (var link in connections)
        {
            _signal.RemoveSinkFromSource(uid, link);
        }

        VerifyReceivers(uid, comp);
    }

    private void OnLinkAttempt(EntityUid uid, BallisticAmmoDistributorComponent comp, ref LinkAttemptEvent args)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(args.Sink, out var ammoProvider))
        {
            args.Cancel();
            _popupSystem.PopupCursor(Loc.GetString("ammo-distributor-no-ammo-provider", ("owner", uid), ("weapon", args.Sink)), args.User ?? uid, PopupType.SmallCaution);
            return;
        }

        if (comp.AmmoSource.Whitelist == null)
            SharedGunSystem.UpdateWhitelist(comp.AmmoSource, ammoProvider.Whitelist);

        if (!_whitelistSystem.WhitelistCompare(comp.AmmoSource.Whitelist, ammoProvider.Whitelist))
        {
            args.Cancel();
            _popupSystem.PopupCursor(Loc.GetString("ammo-distributor-whitelist-fail", ("owner", uid), ("weapon", args.Sink)), args.User ?? uid, PopupType.SmallCaution);
            return;
        }
    }

    private void OnNewLink(EntityUid uid, BallisticAmmoDistributorComponent comp, ref NewLinkEvent args)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(args.Sink, out var ammoProvider))
        {
            /// Somehow got past the <see cref="OnLinkAttempt"/> screening
            _signal.RemoveSinkFromSource(uid, args.Sink);
            return;
        }

        comp.Receivers.Add((args.Sink, ammoProvider));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTime.accumulator += frameTime;
        if (_updateTime.accumulator >= _updateTime.threshold)
        {
            _updateTime.accumulator -= _updateTime.threshold;
            var query = EntityQueryEnumerator<BallisticAmmoDistributorComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var comp, out var xform))
            {
                if (comp.RequireAnchor && !xform.Anchored)
                    continue;

                /// This may eventually need to be dealt with via stopwatch and queue like the <see cref="AtmosphereSystem"/>...
                /// but this will work for now so long as no-one goes crazy with it

                var ev = new AmmoDistributorUpdateEvent();
                RaiseLocalEvent(uid, ev);
            }
        }
    }

    private void OnUpdate(EntityUid uid, BallisticAmmoDistributorComponent comp, ref AmmoDistributorUpdateEvent args)
    {
        VerifyReceivers(uid, comp);

        // Have to double check to appease clients
        comp.AmmoSource ??= EnsureComp<BallisticAmmoProviderComponent>(uid);

        if (comp.AmmoSource.Whitelist == null)
            return;

        if (_gunSystem.GetAmmoCount(uid) <= 0)
        {
            // Not connected to anyone and we're no longer holding ammo, re-open the whitelist
            if (comp.Receivers.Count <= 0)
                SharedGunSystem.UpdateWhitelist(comp.AmmoSource, null);

            return;
        }

        foreach (var receiver in comp.Receivers)
        {
            if (!CheckRange(uid, receiver))
                continue;

            var receiverLacking = _gunSystem.GetAmmoCapacity(receiver) - _gunSystem.GetAmmoCount(receiver);
            if (receiverLacking <= 0)
                continue;

            var ammoEv = new TakeAmmoEvent(Math.Min(comp.TransferAmount, receiverLacking), [], Transform(uid).Coordinates, uid);
            RaiseLocalEvent(uid, ammoEv);

            if (ammoEv.Ammo.Count <= 0)
                break;

            // Makes sure it will only make the loading sound once per receiver instead of once per bullet
            var doneFirst = false;

            foreach (var (bullet, _) in ammoEv.Ammo)
            {
                if (_gunSystem.IsFull(receiver))
                    break;

                if (bullet == null)
                    continue;

                _gunSystem.TryBallisticInsert(receiver, bullet.Value, uid, doneFirst);
                doneFirst = true;
            }
        }
    }

    private bool CheckRange(EntityUid uid, EntityUid receiver)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return false;

        if (_transformSystem.InRange(Transform(uid).Coordinates, Transform(receiver).Coordinates, source.Range))
            return true;

        _signal.RemoveSinkFromSource(uid, receiver);
        return false;
    }

    private void VerifyReceivers(EntityUid uid, BallisticAmmoDistributorComponent comp)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        var connections = source.LinkedPorts.Keys.ToList();
        List<Entity<BallisticAmmoProviderComponent>> badReceivers = [];
        foreach (var receiver in comp.Receivers)
        {
            if (!connections.Contains(receiver.Owner))
                badReceivers.Add(receiver);
        }

        foreach (var badReceiver in badReceivers)
        {
            comp.Receivers.Remove(badReceiver);

            // For some unholy reason it can link to itself, no need to notify when that's undone
            if (badReceiver.Owner != uid)
                _popupSystem.PopupEntity(Loc.GetString("ammo-distributor-connection-lost", ("owner", uid), ("weapon", badReceiver)), uid, PopupType.SmallCaution);
        }
    }
}