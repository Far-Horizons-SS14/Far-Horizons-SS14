using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Prototypes;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
//Far Horizons Start
using Content.Shared.Verbs;
using Content.Server.Hands.Systems;
using Content.Server._FarHorizons.Research;
using Content.Server._FarHorizons.Medical.SurgeryOverhaul.Systems;
using Content.Shared._FarHorizons.Research;
//Far Horizons End
using Content.Shared.Body;
using Content.Shared.Damage.Systems;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly FHResearchSystem _fhResearch = default!; //Far Horizons
    [Dependency] private readonly SurgeryOverhaulSystem _surgeryOverhaul = default!; // Far Horizons
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    private readonly List<EntProtoId> _surgeries = [];
    public override void Initialize()
    {
        base.Initialize();
        InitializeSteps();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<GetVerbsEvent<InteractionVerb>>(AddSurgeryVerb);//Far Horizons

        LoadPrototypes();
    }

    protected override void RefreshUI(EntityUid body)
    {
        if(!_ui.HasUi(body, SurgeryUIKey.Key))
            return;
        var surgeries = new Dictionary<NetEntity, List<(EntProtoId, string suffix, bool isCompleted)>>();
        // Far Horizons start
        if (TryComp<BodyComponent>(body, out var bodyComp) && bodyComp.Organs != null)
        {
            foreach (var part in bodyComp.Organs.ContainedEntities)
            {
                if (!TryComp<OrganComponent>(part, out var organ) ||
                    organ.Category == null ||
                    !_prototypes.TryIndex(organ.Category.Value, out var category) ||
                    !category.SurgeryTargetable)
                    continue;
                
                AddSurgeries(part, body, surgeries);
            }
        }
        // Far Horizons end
        var researchLevel = GetResearchLevel(body);
        _ui.SetUiState(body, SurgeryUIKey.Key, new SurgeryBuiState() { Choices = surgeries, ResearchLevel = researchLevel });
    }

    private void AddSurgeries(EntityUid part, EntityUid body, Dictionary<NetEntity, List<(EntProtoId, string suffix, bool isCompleted)>> surgeries)
    {
        if (!TryComp<SurgeryProgressComponent>(part, out var progress))
        {
            progress = new SurgeryProgressComponent();
            AddComp(part, progress);
        }

        foreach (var surgery in _surgeries)
        {
            if (!_entity.TryGetSingleton(surgery, out var surgeryEnt)
                || !TryComp(surgeryEnt, out SurgeryComponent? surgeryComp)
                || (surgeryComp.Requirement.Count() > 0 && !progress.CompletedSurgeries.Any(x => surgeryComp.Requirement.Contains(x))))
                continue;

            var ev = new SurgeryValidEvent(body, part);

            var isCompleted = progress.CompletedSurgeries.Contains(surgery);
            if (!progress.StartedSurgeries.Contains(surgery)
                && !isCompleted)
            {
                RaiseLocalEvent(surgeryEnt, ref ev);

                if (ev.Cancelled)
                    continue;
            }
            surgeries.GetOrNew(GetNetEntity(part)).Add((surgery, ev.Suffix, isCompleted));
        }
    }
    //Far Horizons Start
    private void AddSurgeryVerb(GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;
        var user = args.User;
        var target = args.Target;
        var item = _handsSystem.GetActiveItem(user);
        if(!HasComp<SurgeryToolComponent>(item))
            return;

        if (!_ui.HasUi(target, SurgeryUIKey.Key) 
            || _ui.IsUiOpen(target, SurgeryUIKey.Key, user) 
            || !HasComp<BodyComponent>(args.Target)) return;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (user == target)
                {
                    _popup.PopupEntity("You can't perform surgery on yourself!", user, user);
                    return;
                }

                _ui.OpenUi(target, SurgeryUIKey.Key, user);

                RefreshUI(target);
            },
            Text = "Begin Surgery",
            IconEntity = GetNetEntity(item),
            Priority = 2,
        };
        args.Verbs.Add(verb);
    }    
    //Far Horizons End
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _surgeries.Clear();

        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<SurgeryComponent>())
                _surgeries.Add(new EntProtoId(entity.ID));
        }
    }
    //FarHorizons Start
    protected override void TryEmoteWithChat(EntityUid body, string? emote)
    {
        if (string.IsNullOrEmpty(emote))
            return;

        _chat.TryEmoteWithChat(body, emote);
    }
    private string GetResearchLevel(EntityUid body)
    {
        if (_surgeryOverhaul.TryGetConnectedResearchServer(body, out var server))
        {
            ProtoId<ResearchTreeUnlockFlagPrototype> AdvSurgeryFlag = "UnlockFlagSurgerySpeed2"; // TODO: make not cringe
            ProtoId<ResearchTreeUnlockFlagPrototype> SurgeryFlag = "UnlockFlagSurgerySpeed1";
            if (_fhResearch.IsFlagUnlocked((server.Value, server.Value.Comp), AdvSurgeryFlag))
                return "Advanced Surgery";
            if (_fhResearch.IsFlagUnlocked((server.Value, server.Value.Comp), SurgeryFlag))
                return "Basic Surgery";
        }
        return "None";
    }
    //FarHorizons End
}
