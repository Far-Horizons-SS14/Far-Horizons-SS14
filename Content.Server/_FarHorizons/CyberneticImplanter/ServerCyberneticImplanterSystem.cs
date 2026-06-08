using System.Linq;
using Content.Server.Popups;
using Content.Shared._FarHorizons.CyberneticImplanter;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Forensics;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Audio;
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
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, CyberneticImplantDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CyberneticImplanterComponent> entity, ref CyberneticImplantDoAfterEvent args)
    {
        //if statment straight from hell 2 Electric Boogaloo, does all the checks to verify target is still valid
        if (args.Handled ||
        args.Cancelled ||
        !TryComp<BodyComponent>(args.Target, out var bodycomponent) ||
        bodycomponent.Organs == null ||
        bodycomponent.Organs.ContainedEntities == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var ImplantOrganComponent, Factory) ||
        ImplantOrganComponent.Category == null ||
        _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo == null)
            return;

        var ConnectsTo = _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo;

        //are there any organs matching the category in ConnectsTo? (might have changed during doafter)
        if (!bodycomponent.Organs.ContainedEntities.Any(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == ConnectsTo))
            return;

        //Is there already an organ of the same category in the body? 
        var ExisitingOrgan = bodycomponent.Organs.ContainedEntities.FirstOrNull(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == ImplantOrganComponent.Category);
        var OrganDestroyed = false;
        //if so remove it first
        if (ExisitingOrgan != null)
        {
            var UsedComponent = AddComp<UsedCyberneticImplanterComponent>(entity);

            if (TryComp(ExisitingOrgan.Value, out MetaDataComponent? metaComp)) //this has to be done before removing the organ to prevent the name of the target being in the entity name
            {
                UsedComponent.Organ = metaComp.EntityName; //dont care if this is null, checks later anyways
                if (TryComp(entity, out MetaDataComponent? implantermetaComp) && TryComp(args.Target, out MetaDataComponent? targetmetaComp))
                    _popupSystem.PopupEntity(Loc.GetString("comp-cyberneticimplanter-organdestroyed", ("implanter", implantermetaComp.EntityName), ("destroyed", metaComp.EntityName), ("target", targetmetaComp.EntityName)), entity, PopupType.LargeCaution);
            }

            if (TryComp(ExisitingOrgan.Value, out VisualOrganMarkingsComponent? markingcomp))
            {
                var species = markingcomp.MarkingData.Group;
                UsedComponent.Species = species.ToString();
            }

            if (!_containers.CanRemove(ExisitingOrgan.Value, bodycomponent.Organs)) //uh oh
                return;
            if (!_containers.Remove(ExisitingOrgan.Value, bodycomponent.Organs)) //uh oh
                return;

            QueueDel(ExisitingOrgan);
            OrganDestroyed = true;
        }

        //spawn the organ and try to insert into target body
        var SpawnedOrgan = Spawn(entity.Comp.ImplantedOrgan);
        if (!_containers.CanInsert(SpawnedOrgan, bodycomponent.Organs)) //uh oh
        {
            QueueDel(SpawnedOrgan); //cleanup
            return;
        }
        if (!_containers.Insert(SpawnedOrgan, bodycomponent.Organs)) //uh oh
        {
            QueueDel(SpawnedOrgan); //cleanup
            return;
        }

        //everything worked! clean up and do cosmetic effects
        var ev = new TransferDnaEvent { Donor = args.Target.Value, Recipient = entity, CanDnaBeCleaned = !OrganDestroyed};
        RaiseLocalEvent(args.Target.Value, ref ev);

        if (!OrganDestroyed && TryComp(SpawnedOrgan, out MetaDataComponent? metadata))
            _popupSystem.PopupEntity(Loc.GetString("comp-cyberneticimplanter-cleanimplant",("implanted", metadata.EntityName)), entity, PopupType.Medium);

        _audio.PlayPredicted(entity.Comp.ImplantEndSound, entity, entity);

        args.Handled = true;

        RemCompDeferred<CyberneticImplanterComponent>(entity); //cyber da world, my final message, goodbye
    }
}