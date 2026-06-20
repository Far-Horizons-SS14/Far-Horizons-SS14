using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.StationEvents.Components;
using Content.Shared._FarHorizons.Body;
using Content.Shared._Starlight.Language.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.Species.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Body;
public sealed partial class BrainTransferSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidCharacterProfileComponent, SexChangedEvent>(OnPlayerSpawn, before:[],  after: [typeof(HumanoidProfileSystem)]);
        SubscribeLocalEvent<BrainExtraComponent, BrainInserted>(OnBrainInserted);
        SubscribeLocalEvent<BrainExtraComponent, BrainRemoved>(OnBrainRemoved);
    }

    private void OnPlayerSpawn(Entity<HumanoidCharacterProfileComponent> ent, ref SexChangedEvent args)
    {
        if(!TryComp<BodyComponent>(ent.Owner, out var body) || body.Organs == null)
            return;
        var brain = body.Organs.ContainedEntities.FirstOrNull(HasComp<BrainComponent>);

        if(brain == null) return;

        if(!HasComp<HumanoidCharacterProfileComponent>(brain.Value))
        {
            var hcpComp = EnsureComp<HumanoidCharacterProfileComponent>(brain.Value);
            hcpComp.Profile = ent.Comp.Profile;
        }
    }

    private void OnBrainInserted(Entity<BrainExtraComponent> ent, ref BrainInserted args)
    {
        foreach(var component in ent.Comp.StoredComponents)
        {
            var comp = _factory.GetComponent(component.Value);
            AddComp(args.Body, comp);
        }
        ent.Comp.StoredComponents.Clear();
        _mind.MakeSentient(args.Body);
    }

    private void OnBrainRemoved(Entity<BrainExtraComponent> ent, ref BrainRemoved args)
    {
        foreach(var component in _mindComponents)
        {
            if (!EntityManager.TryGetComponent(args.Body, component, out var comp))
                continue;

            if (HasComp<NymphComponent>(args.Body) && ent.Comp.StoredComponents.ContainsKey(component.Name))
            {
                RemComp(args.Body, comp);
                continue;
            }

            ent.Comp.StoredComponents[component.Name] = new EntityPrototype.ComponentRegistryEntry(comp, new MappingDataNode());
            RemComp(args.Body, comp);
        }
    }

    private static readonly Type[] _mindComponents =
    {
        typeof(NPCRetaliationComponent),
        typeof(NpcFactionMemberComponent),
        typeof(GhostTakeoverAvailableComponent),
        typeof(GhostRoleComponent),
        typeof(ActiveNPCComponent),
        typeof(SentienceTargetComponent),
        typeof(HTNComponent),
        typeof(LanguageKnowledgeComponent),
        typeof(LanguageSpeakerComponent),
        typeof(ParacusiaComponent)
    };
}