using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Server.Mind;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Body;

public sealed partial class ConnectedOrganSystem : SharedConnectedOrganSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
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

        // Sanity check to avoid endless bleed damage when they reattach surgically removed limbs
        if (HasComp<IncisionOpenComponent>(ent))
            RemCompDeferred<IncisionOpenComponent>(ent);
        
        var connectedCategories = _protoMan.EnumeratePrototypes<OrganCategoryPrototype>()
            .Where(p => p.ConnectsTo == ent.Comp.Category).Select(p => (ProtoId<OrganCategoryPrototype>)p.ID).ToList();
        
        if (connectedCategories.Count == 0) return;
        
        var connectedOrgans = body.Organs.ContainedEntities.Where(p => TryComp<OrganComponent>(p, out var organ) && organ.Category != null && connectedCategories.Contains(organ.Category!.Value)).ToList();

        var connectedComp = EnsureComp<ConnectedOrganComponent>(ent);
        foreach (var connectedOrgan in connectedOrgans)
            if (connectedComp.Organs != null)
                _container.Insert(connectedOrgan, connectedComp.Organs, organTransform, true);

        if(HasComp<VisualOrganComponent>(ent))
        {
            var entName = MetaData(args.Target).EntityName;
            _metaData.SetEntityName(ent, $"{entName}'s {GetPartName(ent.Owner)}", raiseEvents: false);

            if(TryComp<HeadOrganComponent>(ent, out var head))
            {
                head.NameBackup = $"{entName}";
                _metaData.SetEntityName(args.Target, "Unknown", raiseEvents: false);
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
                _container.Insert(connected, body.Organs, Transform(args.Target), true);

        if(HasComp<VisualOrganComponent>(ent))
        {
            _metaData.SetEntityName(ent, $"{GetPartName(ent.Owner)}");

            if(TryComp<HeadOrganComponent>(ent, out var head))
            {
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
}