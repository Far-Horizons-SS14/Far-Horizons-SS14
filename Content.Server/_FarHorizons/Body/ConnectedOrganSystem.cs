using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.HTN;
using Content.Shared._Starlight.Language.Components;
using Content.Shared.Mind.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Server.NPC.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC;
using Content.Server.StationEvents.Components;
using Content.Server.Mind;
using Content.Shared.Traits.Assorted;

namespace Content.Server._FarHorizons.Body;

public sealed partial class ConnectedOrganSystem : SharedConnectedOrganSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConnectedOrganComponent, MapInitEvent>(OnConnectedOrganMapInit);
        SubscribeLocalEvent<ConnectedOrganComponent, OrganGotInsertedEvent>(OnConnectedInserted);
        SubscribeLocalEvent<OrganComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    private void OnConnectedOrganMapInit(Entity<ConnectedOrganComponent> ent, ref MapInitEvent args)
    {
        foreach (var newOrgan in ent.Comp.Roundstart)
            SpawnInContainerOrDrop(newOrgan, ent, ConnectedOrganComponent.ContainerID);
    }

    private void OnOrganRemoved(Entity<OrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            !TryComp(ent, out TransformComponent? organTransform) ||
            !TryComp<BodyComponent>(args.Target, out var body) || 
            body.Organs == null) 
            return;
        
        var connectedCategories = _protoMan.EnumeratePrototypes<OrganCategoryPrototype>()
            .Where(p => p.ConnectsTo == ent.Comp.Category).Select(p => (ProtoId<OrganCategoryPrototype>)p.ID).ToList();
        
        if (connectedCategories.Count == 0) return;
        
        var connectedOrgans = body.Organs.ContainedEntities.Where(p => TryComp<OrganComponent>(p, out var organ) && organ.Category != null && connectedCategories.Contains(organ.Category!.Value)).ToList();

        var connectedComp = EnsureComp<ConnectedOrganComponent>(ent);
        foreach (var connectedOrgan in connectedOrgans)
            if (connectedComp.Organs != null)
            {
                if (HasComp<BrainComponent>(connectedOrgan))
                {
                    CutAndPaste<NPCRetaliationComponent>(args.Target, connectedOrgan);
                    CutAndPaste<NpcFactionMemberComponent>(args.Target, connectedOrgan);
                    CutAndPaste<GhostTakeoverAvailableComponent>(args.Target, connectedOrgan);
                    CutAndPaste<GhostRoleComponent>(args.Target, connectedOrgan);
                    CutAndPaste<ActiveNPCComponent>(args.Target, connectedOrgan);
                    CutAndPaste<SentienceTargetComponent>(args.Target, connectedOrgan);
                    CutAndPaste<HTNComponent>(args.Target, connectedOrgan);
                    CutAndPaste<LanguageKnowledgeComponent>(args.Target, connectedOrgan);
                    CutAndPaste<LanguageSpeakerComponent>(args.Target, connectedOrgan);
                    CutAndPaste<ParacusiaComponent>(args.Target, connectedOrgan);
                }
                _container.Insert(connectedOrgan, connectedComp.Organs, organTransform, true);
            }

        if(HasComp<VisualOrganComponent>(ent))
        {
            var entName = MetaData(args.Target).EntityName;
            _metaData.SetEntityName(ent, $"{entName}'s {GetPartName(ent.Owner)}");

            if(TryComp<HeadOrganComponent>(ent, out var head))
            {
                CutAndPaste<IdentityComponent>(args.Target, ent);
                head.NameBackup = $"{entName}";
                _metaData.SetEntityName(args.Target, "Unknown");
            }
        }
    }

    private void OnConnectedInserted(Entity<ConnectedOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (ent.Comp.Organs is not { Count: > 0 } || 
            !TryComp<BodyComponent>(args.Target, out var body) || 
            body.Organs == null) 
            return;

        if (!ent.Comp.Initialized) return;

        foreach (var connected in ent.Comp.Organs.ContainedEntities.ToList()
                     .Where(connected => _container.CanInsert(connected, body.Organs)))
        {

            if (HasComp<BrainComponent>(connected))
            {
                CutAndPaste<NPCRetaliationComponent>(connected, args.Target);
                CutAndPaste<NpcFactionMemberComponent>(connected, args.Target);
                CutAndPaste<GhostTakeoverAvailableComponent>(connected, args.Target);
                CutAndPaste<GhostRoleComponent>(connected, args.Target);
                CutAndPaste<ActiveNPCComponent>(connected, args.Target);
                CutAndPaste<SentienceTargetComponent>(connected, args.Target);
                CutAndPaste<HTNComponent>(connected, args.Target);
                CutAndPaste<LanguageKnowledgeComponent>(connected, args.Target);
                CutAndPaste<LanguageSpeakerComponent>(connected, args.Target);
                CutAndPaste<ParacusiaComponent>(connected, args.Target);
                            
                if(TryComp<MindContainerComponent>(connected, out var mind) && mind != null)
                    _mind.MakeSentient(args.Target);
            }
            _container.Insert(connected, body.Organs, Transform(args.Target), true);
        } 

        if(HasComp<VisualOrganComponent>(ent))
        {
            _metaData.SetEntityName(ent, $"{GetPartName(ent.Owner)}");

            if(TryComp<HeadOrganComponent>(ent, out var head))
            {
                CutAndPaste<IdentityComponent>(ent, args.Target);
                _metaData.SetEntityName(args.Target, head.NameBackup);
                head.NameBackup = "";
            }
        }
    }

    private string GetPartName(EntityUid ent)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Category == null)
            return "";

        return (string)organ.Category switch
        {
            "Head" => "Head",
            "AnimalHead" => "Head",
            "Torso" => "Torso",
            "ArmRight" => "Right Arm",
            "HandRight" => "Right Hand",
            "ArmLeft" => "Left Arm",
            "HandLeft" => "Left Hand",
            "LegRight" => "Right Leg",
            "FootRight" => "Right Foot",
            "LegLeft" => "Left Leg",
            "FootLeft" => "Left Foot",
            _ => "",
        };
    }

    private void CutAndPaste<T>(EntityUid from, EntityUid to) where T : IComponent
    {
        if (!TryComp<T>(from, out var comp))
            return;

        CopyComp(from, to, comp);
        RemComp<T>(from);
    }
}