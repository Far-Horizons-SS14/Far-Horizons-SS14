using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.Power.EntitySystems;
using Content.Shared._FarHorizons.NPC;
using Content.Shared._FarHorizons.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.NPC;

public sealed partial class SimpleBotConfigurationSystem : EntitySystem
{
    private const int MaxPatrolBeaconLength = 64;

    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SpeechSystem _speech = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleBotConfigurationComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SimpleBotConfigurationComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SimpleBotConfigurationComponent, BoundUserInterfaceMessageAttempt>(OnMessageAttempt);
        SubscribeLocalEvent<SimpleBotConfigurationComponent, SimpleBotConfigurationBuiMessage>(OnConfigurationMessage);
        SubscribeLocalEvent<SimpleBotConfigurationComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentStartup(Entity<SimpleBotConfigurationComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<HTNComponent>(ent.Owner, out var htn))
        {
            // Yes, I know the simple bots can't perform interact actions; this is needed
            // so that the pathfinding system paths them through doors.
            htn.Blackboard.SetValue(NPCBlackboard.NavInteract, true);
            htn.Blackboard.SetValue(NPCBlackboard.NavAccess, true);
        }

        SetPowered(ent, ent.Comp.Powered);
        SetSpeechEnabled(ent, ent.Comp.SpeechEnabled);
    }

    private void OnGetVerbs(Entity<SimpleBotConfigurationComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        if (!this.IsPowered(ent.Owner, EntityManager) ||
            !args.CanAccess ||
            (!HasAiAccess(ent.Owner, user) &&
             (!args.CanInteract || !TryComp<WiresPanelComponent>(ent.Owner, out var panel) || !panel.Open)))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("simple-bot-configuration-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => _ui.TryOpenUi(ent.Owner, SimpleBotConfigurationUiKey.Key, user),
        });
    }

    private void OnMessageAttempt(Entity<SimpleBotConfigurationComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            args.Cancel();
            return;
        }

        if (HasAiAccess(ent.Owner, args.Actor))
            return;

        if (!TryComp<WiresPanelComponent>(ent.Owner, out var panel) ||
            !panel.Open ||
            !_interaction.InRangeUnobstructed(args.Actor, ent.Owner))
        {
            args.Cancel();
        }
    }

    private void OnConfigurationMessage(Entity<SimpleBotConfigurationComponent> ent, ref SimpleBotConfigurationBuiMessage args)
    {
        if (args.PatrolBeacon.Length > MaxPatrolBeaconLength)
            return;

        SetPatrol(ent, args.PatrolMode, args.PatrolBeacon);
        SetSpeechEnabled(ent, args.SpeechEnabled);
        SetPowered(ent, args.Powered);
    }

    private bool HasAiAccess(EntityUid bot, EntityUid user)
    {
        return HasComp<StationAiHeldComponent>(user) &&
               TryComp<StationAiWhitelistComponent>(bot, out var whitelist) &&
               whitelist.Enabled;
    }

    public void SetPowered(Entity<SimpleBotConfigurationComponent> ent, bool powered)
    {
        ent.Comp.Powered = powered;

        UpdateHtnPower(ent);

        Dirty(ent);
    }

    public void SetPatrol(Entity<SimpleBotConfigurationComponent> ent, SimpleBotPatrolMode mode, string beacon)
    {
        ent.Comp.PatrolMode = mode;
        ent.Comp.PatrolBeacon = beacon.Trim();
        Dirty(ent);
    }

    private void OnPowerChanged(Entity<SimpleBotConfigurationComponent> ent, ref PowerChangedEvent args)
    {
        UpdateHtnPower(ent);
    }

    public void SetSpeechEnabled(Entity<SimpleBotConfigurationComponent> ent, bool enabled)
    {
        ent.Comp.SpeechEnabled = enabled;

        if (TryComp<SpeechComponent>(ent.Owner, out var speech))
            _speech.SetSpeech(ent.Owner, enabled, speech);

        if (TryComp<HTNComponent>(ent.Owner, out var htn))
        {
            if (enabled)
                htn.Blackboard.SetValue(SimpleBotBlackboard.SpeechEnabled, true);
            else
                htn.Blackboard.Remove<bool>(SimpleBotBlackboard.SpeechEnabled);
        }

        Dirty(ent);
    }

    private void UpdateHtnPower(Entity<SimpleBotConfigurationComponent> ent)
    {
        if (TryComp<HTNComponent>(ent.Owner, out var htn))
            _htn.SetHTNEnabled((ent, htn), ent.Comp.Powered && this.IsPowered(ent.Owner, EntityManager));
    }
}
