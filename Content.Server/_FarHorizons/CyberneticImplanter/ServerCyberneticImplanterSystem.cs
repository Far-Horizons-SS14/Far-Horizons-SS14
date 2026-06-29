using System.Linq;
using Content.Server.Popups;
using Content.Shared._FarHorizons.CyberneticImplanter;
using Content.Shared.Body;
using Content.Shared.Forensics;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.CyberneticImplanter;

public sealed class ServerCyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, CyberneticImplanterDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CyberneticImplanterComponent> entity, ref CyberneticImplanterDoAfterEvent args)
    {
        if (args.Handled)
            return;

        _visualizer.SetData(entity, CyberneticImplanterVisuals.State, CyberneticImplanterState.Icon); //set to the icon state for when any of the following checks fail

        //if statment straight from hell 2 Electric Boogaloo, does all the checks to verify target is still valid
        if (args.Cancelled ||
            !TryComp<BodyComponent>(args.Target, out var bodycomponent) ||
            bodycomponent.Organs == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var implantOrganComp, Factory) ||
        implantOrganComp.Category == null ||
        !(_protoManager.Index<OrganCategoryPrototype>(implantOrganComp.Category) is { ConnectsTo: not null } organCategory))
            return;

        var connectsTo = organCategory.ConnectsTo;

        //are there any organs matching the category in ConnectsTo? (might have changed during doafter)
        if (!bodycomponent.Organs.ContainedEntities.Any(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == connectsTo))
            return;

        //Is there already an organ of the same category in the body? 
        var exisitingOrgan = bodycomponent.Organs.ContainedEntities.FirstOrNull(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == implantOrganComp.Category);
        var organDestroyed = false;
        //if so remove it first
        if (exisitingOrgan != null)
        {
            var usedComponent = AddComp<UsedCyberneticImplanterComponent>(entity);

            if (TryComp(exisitingOrgan.Value, out MetaDataComponent? metaComp)) //this has to be done before removing the organ to prevent the name of the target being in the entity name
            {
                usedComponent.Organ = metaComp.EntityName; //dont care if this is null, checks later anyways
                if (TryComp(entity, out MetaDataComponent? implantermetaComp) && TryComp(args.Target, out MetaDataComponent? targetmetaComp))
                    _popupSystem.PopupEntity(Loc.GetString("comp-cyberneticimplanter-organdestroyed", ("implanter", implantermetaComp.EntityName), ("destroyed", metaComp.EntityName), ("target", targetmetaComp.EntityName)), entity, PopupType.LargeCaution);
            }

            if (TryComp(exisitingOrgan.Value, out VisualOrganMarkingsComponent? markingcomp))
            {
                var species = markingcomp.MarkingData.Group;
                if (species != "")
                    usedComponent.Species = species.ToString() + " ";
            }

            if (!_containers.CanRemove(exisitingOrgan.Value, bodycomponent.Organs)) //uh oh
                return;
            if (!_containers.Remove(exisitingOrgan.Value, bodycomponent.Organs)) //uh oh
                return;

            QueueDel(exisitingOrgan);
            _visualizer.SetData(entity, CyberneticImplanterVisuals.State, CyberneticImplanterState.Gored);
            organDestroyed = true;
        }

        //spawn the organ and try to insert into target body
        // var spawnedOrgan = Spawn(entity.Comp.ImplantedOrgan);
        // if (!_containers.CanInsert(spawnedOrgan, bodycomponent.Organs)) //uh oh
        // {
        //     QueueDel(spawnedOrgan); //cleanup
        //     return;
        // }
        // if (!_containers.Insert(spawnedOrgan, bodycomponent.Organs)) //uh oh
        // {
        //     QueueDel(spawnedOrgan); //cleanup
        //     return;
        // }

        if (!TrySpawnInContainer(entity.Comp.ImplantedOrgan, args.Target.Value, bodycomponent.Organs.ID, out var spawnedOrgan)) //works
            return;

        //everything worked! clean up and do cosmetic effects
        var ev = new TransferDnaEvent { Donor = args.Target.Value, Recipient = entity, CanDnaBeCleaned = !organDestroyed };
        RaiseLocalEvent(args.Target.Value, ref ev);

        if (!organDestroyed && TryComp(spawnedOrgan, out MetaDataComponent? metadata))
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-cyberneticimplanter-cleanimplant", ("implanted", metadata.EntityName)), entity, PopupType.Medium);
            _visualizer.SetData(entity, CyberneticImplanterVisuals.State, CyberneticImplanterState.Used);
        }

        _audio.PlayPredicted(entity.Comp.ImplantEndSound, entity, entity);

        args.Handled = true;

        RemCompDeferred<CyberneticImplanterComponent>(entity); //cyber da world, my final message, goodbye
    }
}