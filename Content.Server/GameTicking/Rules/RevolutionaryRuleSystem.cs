using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Cuffs.Components;
using Robust.Shared.Player;

#region Starlight
using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Containers;
using Content.Server.Implants;
using Content.Server.Inventory;
using Content.Server.StationEvents.Components;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared._Starlight.Shadekin;
using Content.Shared._Starlight.Silicons.Borgs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
#endregion Starlight

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed partial class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private EuiManager _euiMan = default!;
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private ChatSystem _chatSystem = default!; // Starlight
    [Dependency] private SharedAudioSystem _audioSystem = default!; // Starlight
    [Dependency] private SpecialLobbyContentSystem _specialLobbyContent = default!; // Starlight
    [Dependency] private AlertLevelSystem _alert = default!; // Starlight
    [Dependency] private StoreSystem _storeSystem = default!; // Far Horizons: USSP uplinks use detached stores.
    [Dependency] private SubdermalImplantSystem _implantSystem = default!; // Far Horizons
    [Dependency] private InventorySystem _inventorySystem = default!; // Far Horizons
    [Dependency] private SharedHandsSystem _handsSystem = default!; // Far Horizons
    [Dependency] private USSPUplinkSystem _uplinkSystem = default!; // Far Horizons

    // Starlight
    private readonly SoundSpecifier RevEndGlobalSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/sov_choir_global.ogg");
    private readonly SoundSpecifier RevEndSound = new SoundPathSpecifier("/Audio/_Starlight/Misc/rev_end.ogg");


    //Used in OnPostFlash, no reference to the rule component is available
    public readonly ProtoId<NpcFactionPrototype> RevolutionaryNpcFaction = "Revolutionary";
    public readonly ProtoId<NpcFactionPrototype> RevPrototypeId = "Rev";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);

        SubscribeLocalEvent<RevolutionaryRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<RevolutionaryRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntitySelected); // Starlight
    }

    // Starlight Start
    private void OnAfterAntagEntitySelected(EntityUid uid, RevolutionaryRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        // Send a custom briefing with the character's name
        var name = Identity.Name(args.EntityUid, EntityManager);
        _antag.SendBriefing(args.Session, Loc.GetString("head-rev-role-greeting", ("name", name)), Color.LightYellow, new SoundPathSpecifier("/Audio/Ambience/Antag/headrev_start.ogg"));
    }
    // Starlight End

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }

    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (component.CommandCheck <= _timing.CurTime)
        {
            component.CommandCheck = _timing.CurTime + component.TimerWait;

            //starlight, check if revs have lost
            if (CheckRevsLose())
            {
                GameTicker.EndGameRule(uid, gameRule);
            }

            if (CheckCommandLose())
            {
                // Starlight Start

                _roundEnd.CancelRoundEndCountdown(null, null, false);

                // Play the revolutionary end sound globally
                var filter = Filter.Broadcast();
                _audioSystem.PlayGlobal(RevEndSound, filter, false);

                // First, end the game rule
                GameTicker.EndGameRule(uid, gameRule);

                // Check if the emergency shuttle is already called (not just arrived)
                if (_roundEnd.IsRoundEndRequested())
                {
                    // If the shuttle is already called, we need to recall it
                    // Cancel the current shuttle call - force it with false for checkCooldown
                    _roundEnd.CancelRoundEndCountdown(null, null, false);
                }

                // Use a safer approach for scheduling the announcements
                // Schedule the first announcement after 7 seconds
                Timer.Spawn(TimeSpan.FromSeconds(7), () =>
                {
                    try
                    {
                        // Send Central Command announcement
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("central-command-revolution-announcement"),
                            Loc.GetString("central-command-sender"),
                            true,
                            new SoundPathSpecifier("/Audio/_Starlight/Announcements/announce_broken.ogg"),
                            Color.Red
                        );

                        // Remove event schedulers
                        RemoveEventSchedulers();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error during first announcement: {ex}");
                    }
                });

                // Schedule the second announcement separately after 22 seconds (7 + 15)
                Timer.Spawn(TimeSpan.FromSeconds(32), () =>
                {
                    try
                    {
                        // Send Soviet People's Commissariat announcement
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("soviet-commissariat-revolution-announcement"),
                            Loc.GetString("soviet-commissariat-sender"),
                            true,
                            new SoundPathSpecifier("/Audio/_Starlight/Announcements/sov_announce.ogg"),
                            Color.Yellow
                        );

                        // Wait a short time to ensure the announcement is heard before ending the round
                        Timer.Spawn(TimeSpan.FromSeconds(4), () =>
                        {
                            // STARLIGHT: Set special lobby content for revolutionary victory using the modular system
                            _specialLobbyContent.SetSpecialLobbyContent(uid);

                            // End the round
                            // _audioSystem.PlayGlobal("/Audio/_Starlight/Misc/sov_win.ogg", filter, false);
                            _roundEnd.EndRound();
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error during second announcement: {ex}");
                        // Still try to end the round even if the announcement fails
                        _roundEnd.EndRound();
                    }
                });
                // Starlight End
            }
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var revsLost = CheckRevsLose();
        var commandLost = CheckCommandLose();
        // This is (revsLost, commandsLost) concatted together
        // (moony wrote this comment idk what it means)
        var index = (commandLost ? 1 : 0) | (revsLost ? 2 : 0);
        args.AddLine(Loc.GetString(Outcomes[index]));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("rev-headrev-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            _role.MindHasRole<RevolutionaryRoleComponent>(mind, out var role);
            var count = CompOrNull<RevolutionaryRoleComponent>(role)?.ConvertedCount ?? 0;

            args.AddLine(Loc.GetString("rev-headrev-name-user",
                ("name", name),
                ("username", data.UserName),
                ("count", count)));

            // TODO: someone suggested listing all alive? revs maybe implement at some point
        }
        args.AddLine("");
    }

    private void OnGetBriefing(EntityUid uid, RevolutionaryRoleComponent comp, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        var head = HasComp<HeadRevolutionaryComponent>(ent);
        args.Append(Loc.GetString(head ? "head-rev-briefing" : "rev-briefing"));
    }

    /// <summary>
    /// Starlight: Finds the USSP uplink for a given user, checking implants, inventory, and
    /// held items. If the user is a head revolutionary, it will also cache the uplink in their implant component.
    /// </summary>
    private EntityUid? FindUSSPUplink(EntityUid user)
    {
        // If this is a head revolutionary, check whether their cached uplink is still valid.
        if (TryComp<HeadRevolutionaryImplantComponent>(user, out var implantComp) && implantComp.ImplantUid != null)
        {
            var uplink = implantComp.ImplantUid.Value;
            if (Exists(uplink) &&
                ((HasComp<USSPUplinkImplantComponent>(uplink) &&
                  CompOrNull<USSPUplinkOwnerComponent>(uplink)?.OwnerUid == user) ||
                 MetaData(uplink).EntityPrototype?.ID == "USSPUplinkRadioPreset") &&
                _storeSystem.TryGetStore(uplink, out _))
            {
                return uplink;
            }
        }

        // Check for a USSP uplink implant in the user's implants.
        if (_implantSystem.TryGetImplants(user, out var implants))
        {
            foreach (var implant in implants)
            {
                if (!HasComp<USSPUplinkImplantComponent>(implant) ||
                    !_storeSystem.TryGetStore(implant, out _))
                    continue;

                if (HasComp<HeadRevolutionaryComponent>(user))
                    EnsureComp<HeadRevolutionaryImplantComponent>(user).ImplantUid = implant;

                return implant;
            }
        }

        // Search container slots for a carried USSP radio uplink.
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var slotEntity))
            {
                if (slotEntity.ContainedEntity is not { } contained ||
                    MetaData(contained).EntityPrototype?.ID != "USSPUplinkRadioPreset" ||
                    !_storeSystem.TryGetStore(contained, out _))
                    continue;

                if (HasComp<HeadRevolutionaryComponent>(user))
                    EnsureComp<HeadRevolutionaryImplantComponent>(user).ImplantUid = contained;

                return contained;
            }
        }

        // Search held items for a carried USSP radio uplink.
        foreach (var held in _handsSystem.EnumerateHeld(user))
        {
            if (MetaData(held).EntityPrototype?.ID != "USSPUplinkRadioPreset" ||
                !_storeSystem.TryGetStore(held, out _))
                continue;

            if (HasComp<HeadRevolutionaryComponent>(user))
                EnsureComp<HeadRevolutionaryImplantComponent>(user).ImplantUid = held;

            return held;
        }

        return null;
    }
    // STARLIGHT END

    /// <summary>
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    private void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        if (uid != ev.User || !ev.Melee)
            return;

        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target);

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            HasComp<BrighteyeComponent>(ev.Target) || // Starlight Addtion
            HasComp<BorgChassisComponent>(ev.Target) || // Starlight Addition - Borgis should be emagged not flashed
            !HasComp<HumanoidProfileComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target) ||
            !HasComp<RevolutionaryConverterComponent>(ev.Used))
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, RevolutionaryNpcFaction);
        var revComp = EnsureComp<RevolutionaryComponent>(ev.Target);

        // Starlight: Add a component to track which head revolutionary converted this revolutionary
        if (ev.User != null && HasComp<HeadRevolutionaryComponent>(ev.User.Value))
        {
            var converterComp = EnsureComp<RevolutionaryConvertedByComponent>(ev.Target);
            converterComp.ConverterUid = ev.User.Value;
        }
        // Starlight End

        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");

            // STARLIGHT START
            var storeSystem = EntityManager.System<StoreSystem>();

            // Add Telebond to the converter's uplink
            if (HasComp<HeadRevolutionaryComponent>(ev.User.Value))
            {
                // Find the head revolutionary's uplink
                var uplinkUid = FindUSSPUplink(ev.User.Value);
                var createdUplink = false;

                // If no uplink was found, create one
                if (uplinkUid == null)
                {
                    var uplinkImplant = EntityManager.System<SubdermalImplantSystem>()
                        .AddImplant(ev.User.Value, "USSPUplinkImplant");
                    if (uplinkImplant != null)
                    {
                        uplinkUid = uplinkImplant.Value;
                        createdUplink = true;
                    }
                }

                if (uplinkUid != null)
                {
                    var currencyToAdd = new Dictionary<string, FixedPoint2> { { "Telebond", FixedPoint2.New(2) } }; /// FH - MOAR
                    if (!createdUplink &&
                        _storeSystem.TryGetStore(uplinkUid.Value, out var storeComp) &&
                        storeComp is { } resolvedStore)
                        storeSystem.TryAddCurrency(currencyToAdd, resolvedStore.Owner, resolvedStore.Comp);

                    var finalTelebond = FixedPoint2.Zero;
                    if (_storeSystem.TryGetStore(uplinkUid.Value, out var finalStoreComp) &&
                        finalStoreComp is { } resolvedFinalStore)
                        finalTelebond = resolvedFinalStore.Comp.Balance.GetValueOrDefault("Telebond", FixedPoint2.Zero);

                    // Show popup to the head revolutionary (private)
                    _popup.PopupEntity(Loc.GetString($"+1 Telebond (Total: {finalTelebond})"), ev.User.Value, ev.User.Value, PopupType.Medium);

                    // If the uplink is implanted in someone else, show them a popup too
                    if (TryComp<SubdermalImplantComponent>(uplinkUid.Value, out var implant) &&
                        implant.ImplantedEntity != null &&
                        implant.ImplantedEntity.Value != ev.User.Value)
                    {
                        _popup.PopupEntity(Loc.GetString($"+1 Telebond (Total: {finalTelebond}) (for {Identity.Name(ev.User.Value, EntityManager)})"),
                            implant.ImplantedEntity.Value, implant.ImplantedEntity.Value, PopupType.Large);
                    }

                    // Also show a popup to any revolutionary who has this uplink's entity UID stored in their HeadRevolutionaryImplantComponent
                    var revQuery = EntityQueryEnumerator<RevolutionaryComponent, HeadRevolutionaryImplantComponent>();
                    while (revQuery.MoveNext(out var headRev, out _, out var revImplantComp))
                    {
                        if (revImplantComp.ImplantUid == uplinkUid &&
                            headRev != ev.User.Value &&
                            (implant == null || implant.ImplantedEntity == null || headRev != implant.ImplantedEntity.Value))
                        {
                            _popup.PopupEntity(Loc.GetString($"+1 Telebond (for {Identity.Name(ev.User.Value, EntityManager)})"),
                                headRev, headRev, PopupType.Large);
                        }
                    }

                    // Also check for any revolutionaries who have an implant with this uplink
                    var allRevs = EntityQueryEnumerator<RevolutionaryComponent>();
                    while (allRevs.MoveNext(out var revolutionary, out var revolutionaryComponent))
                    {
                        // Skip the head revolutionary who did the conversion
                        if (revolutionary == ev.User.Value)
                            continue;

                        // Skip the implanted entity if we already showed them a popup
                        if (implant != null && implant.ImplantedEntity != null && revolutionary == implant.ImplantedEntity.Value)
                            continue;

                        // Check if this revolutionary has an implant
                        if (_implantSystem.TryGetImplants(revolutionary, out var implants))
                        {
                            foreach (var revImplant in implants)
                            {
                                // Check if this implant is the same as the uplink or has the same owner
                                if (revImplant == uplinkUid ||
                                    (TryComp<USSPUplinkOwnerComponent>(revImplant, out var ownerComp) &&
                                     ownerComp.OwnerUid == ev.User.Value))
                                {
                                    _popup.PopupEntity(Loc.GetString($"+1 Telebond (for {Identity.Name(ev.User.Value, EntityManager)})"),
                                        revolutionary, revolutionary, PopupType.Medium);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Add Conversion to ALL head revolutionary uplinks with a 1-second delay
            // This prevents the Conversion popup from appearing at the same time as the Telebond popup
            Timer.Spawn(TimeSpan.FromSeconds(1), () =>
            {
                var headUplinks = new Dictionary<EntityUid, EntityUid>();
                var allHeadRevs = EntityQueryEnumerator<HeadRevolutionaryComponent>();
                while (allHeadRevs.MoveNext(out var head, out _))
                {
                    if (FindUSSPUplink(head) is { } uplink)
                        headUplinks[head] = uplink;
                }

                _uplinkSystem.AddConversionToAllHeadRevs(storeSystem, headUplinks);
            });

            // STARLIGHT END

            if (_mind.TryGetMind(ev.User.Value, out var revMindId, out _))
            {
                if (_role.MindHasRole<RevolutionaryRoleComponent>(revMindId, out var role))
                {
                    role.Value.Comp2.ConvertedCount++;
                    Dirty(role.Value.Owner, role.Value.Comp2);
                }
            }
        }

        if (mindId == default || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleRevolutionary");
        }

        if (mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
            _antag.SendBriefing(session, Loc.GetString("rev-role-greeting", ("name", Identity.Name(ev.Target, EntityManager))), Color.LightYellow, revComp.RevStartSound); // STARLIGHT
    }

    //TODO: Enemies of the revolution
    private void OnCommandMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose();
    }

    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    private bool CheckCommandLose()
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }

        return IsGroupDetainedOrDead(commandList, true, true, true);
    }

    /// <summary>
    /// Starlight: Removes various event schedulers from the game rules.
    /// </summary>
    private void RemoveEventSchedulers()
    {
        // Remove BasicStationEventScheduler
        var basicSchedulers = EntityQueryEnumerator<BasicStationEventSchedulerComponent>();
        while (basicSchedulers.MoveNext(out var scheduler, out _))
        {
            RemComp<BasicStationEventSchedulerComponent>(scheduler);
        }

        // Remove RampingStationEventScheduler
        var rampingSchedulers = EntityQueryEnumerator<RampingStationEventSchedulerComponent>();
        while (rampingSchedulers.MoveNext(out var scheduler, out _))
        {
            RemComp<RampingStationEventSchedulerComponent>(scheduler);
        }
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose();
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var uid, out _, out _))
        {
            headRevList.Add(uid);
        }

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        // Cuffing Head Revs is not enough - they must be killed.
        if (IsGroupDetainedOrDead(headRevList, false, false, false))
        {
            // STARLIGHT: Delete all USSP uplinks and turn supply rifts and SKB implanters to ash
            DeleteUplinksTurnItemsToAsh();

            var rev = AllEntityQuery<RevolutionaryComponent, MindContainerComponent>();
            while (rev.MoveNext(out var uid, out _, out var mc))
            {
                if (HasComp<HeadRevolutionaryComponent>(uid)||HasComp<SiliconLawBoundComponent>(uid)) // Starlight silicons cannot be deconverted
                    continue;

                // Play the deconversion sound for the revolutionary
                _audioSystem.PlayGlobal(RevEndSound, Filter.Entities(uid), false, AudioParams.Default.WithVolume(0f));

                _npcFaction.RemoveFaction(uid, RevolutionaryNpcFaction);
                _stun.TryUpdateParalyzeDuration(uid, stunTime);
                RemCompDeferred<RevolutionaryComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Name(uid, EntityManager))), uid); //STARLIGHT
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

                if (!_mind.TryGetMind(uid, out var mindId, out var mind, mc))
                    continue;

                // remove their antag role
                _role.MindRemoveRole<RevolutionaryRoleComponent>(mindId);

                // make it very obvious to the rev they've been deconverted since
                // they may not see the popup due to antag and/or new player tunnel vision
                if (_player.TryGetSessionById(mind.UserId, out var session))
                    _euiMan.OpenEui(new DeconvertedEui(), session);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// STARLIGHT: Deletes all USSP uplinks and turns supply rifts and SKB implanters to ash when all head revolutionaries are dead.
    /// </summary>
    private void DeleteUplinksTurnItemsToAsh()
    {
        // Find and delete all USSP uplinks
        EntityUid uid = default; // This sucks. Has to be a better way.
        var uplinkQuery = AllEntityQuery<MetaDataComponent>();
        var uplinksToDelete = new List<EntityUid>();

        while (uplinkQuery.MoveNext(out var uplink, out var metadata))
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplant")
            {
                uplinksToDelete.Add(uplink);
            }
        }

        // Delete all uplinks
        foreach (var uplink in uplinksToDelete)
        {
            if (Exists(uplink))
            {
                QueueDel(uplink);
            }
        }

        // Find all supply rifts and collect them for deletion
        var riftsToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var riftQuery = EntityQueryEnumerator<RevSupplyRiftComponent, TransformComponent>();

        while (riftQuery.MoveNext(out var rift, out _, out var transform))
        {
            riftsToDelete.Add((rift, transform.Coordinates));
        }

        // Process all supply rifts
        foreach (var (entity, coordinates) in riftsToDelete)
        {
            if (Exists(entity))
            {
                // Spawn ash at the rift's location
                Spawn("Ash", coordinates);

                if (uid == default)
                {
                    var xform = Transform(entity);
                    var station = _stationSystem.GetStationInMap(xform.MapID);
                    if (station != null)
                    {
                        uid = station.Value;
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("centcomm-revs-alldead"),
                            Loc.GetString("cmd-announce-sender"));
                        _alert.SetLevel(station.Value, "green", true, true, true);
                    }
                }

                // Delete the rift
                QueueDel(entity);
            }
        }

        // Find all SKB implanters and collect them for deletion
        var implantersToDelete = new List<(EntityUid Entity, Robust.Shared.Map.EntityCoordinates Coordinates)>();
        var implanterQuery = AllEntityQuery<MetaDataComponent, TransformComponent>();

        while (implanterQuery.MoveNext(out var implanter, out var metadata, out var transform))
        {
            if (metadata.EntityPrototype?.ID == "USSPUplinkImplanter")
            {
                implantersToDelete.Add((implanter, transform.Coordinates));
            }
        }

        // Process all SKB implanters
        foreach (var (entity, coordinates) in implantersToDelete)
        {
            if (Exists(entity))
            {
                // Spawn ash at the implanter's location
                Spawn("Ash", coordinates);

                // Delete the implanter
                QueueDel(entity);
            }
        }
    }

    /// <summary>
    /// Will take a group of entities and check if these entities are alive, dead or cuffed.
    /// </summary>
    /// <param name="list">The list of the entities</param>
    /// <param name="checkOffStation">Bool for if you want to check if someone is in space and consider them missing in action. (Won't check when emergency shuttle arrives just in case)</param>
    /// <param name="countCuffed">Bool for if you don't want to count cuffed entities.</param>
    /// <param name="countRevolutionaries">Bool for if you want to count revolutionaries.</param>
    /// <returns></returns>
    private bool IsGroupDetainedOrDead(List<EntityUid> list, bool checkOffStation, bool countCuffed, bool countRevolutionaries)
    {
        var gone = 0;

        foreach (var entity in list)
        {
            if (TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0 && countCuffed)
            {
                gone++;
                continue;
            }

            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    gone++;
                    continue;
                }

                if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    gone++;
                    continue;
                }
            }
            //If they don't have the MobStateComponent they might as well be dead.
            else
            {
                gone++;
                continue;
            }

            if ((HasComp<RevolutionaryComponent>(entity) || HasComp<HeadRevolutionaryComponent>(entity)) && countRevolutionaries)
            {
                gone++;
                continue;
            }
        }

        return gone == list.Count || list.Count == 0;
    }

    private static readonly string[] Outcomes =
    {
        // revs survived and heads survived... how
        "rev-reverse-stalemate",
        // revs won and heads died
        "rev-won",
        // revs lost and heads survived
        "rev-lost",
        // revs lost and heads died
        "rev-stalemate"
    };
}
