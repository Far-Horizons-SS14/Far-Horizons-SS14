using Content.Server.Mind;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Species.Components;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
//Far Horizons Start
using Robust.Shared.Utility;
using Content.Shared.Body.Components; 
using Content.Shared._FarHorizons.Body; 
using System.Linq; 
//Far Horizons End

namespace Content.Server.Species.Systems;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, OrganGotRemovedEvent>(OnRemovedFromPart);
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.Target))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        // Get the organs' position & spawn a nymph there
        var coords = Transform(uid).Coordinates;
        var nymph = SpawnAtPosition(entityProto.ID, coords);

        if (HasComp<ZombieComponent>(args.Target)) // Zombify the new nymph if old one is a zombie
            _zombie.ZombifyEntity(nymph);

        // Move the mind if there is one and it's supposed to be transferred
        if (comp.TransferMind && _mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            //Far Horizons Start
            if(TryComp<BodyComponent>(nymph, out var body) && body.Organs != null)
            {
                var nymphBrain = body.Organs.ContainedEntities.FirstOrNull(HasComp<BrainComponent>);

                if(nymphBrain != null)
                {
                    if(TryComp<HumanoidCharacterProfileComponent>(uid, out var hcpComp))
                        EnsureComp<HumanoidCharacterProfileComponent>(nymphBrain.Value).Profile = hcpComp.Profile;
                    if(TryComp<BrainExtraComponent>(uid, out var brainComp))
                        EnsureComp<BrainExtraComponent>(nymphBrain.Value).StoredComponents = brainComp.StoredComponents; 
                }
            }
            //Far Horizons End
            _mindSystem.TransferTo(mindId, nymph, mind: mind);
        }

        // Delete the old organ
        QueueDel(uid);
    }
}
