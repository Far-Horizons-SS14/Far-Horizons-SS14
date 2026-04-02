using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.LimbDamage.Effect;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectDetach : ILimbDamageEffect
{
    private static readonly SoundSpecifier? _gibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (limb.Comp.Organ?.Body == null)
            return;

        var body = limb.Comp.Organ.Body.Value;

        entMan.System<SharedContainerSystem>().TryRemoveFromContainer(limb.Owner);
        entMan.System<SharedAudioSystem>().PlayPredicted(_gibSound, body, null);
    }
        
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectDestroy : ILimbDamageEffect
{
    private static readonly SoundSpecifier? _gibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (limb.Comp.Organ?.Body == null ||
            !entMan.TryGetComponent<BodyComponent>(limb.Comp.Organ.Body.Value, out var body))
            DestroyLimbOutsideOfBody(limb, entMan, protoMan);
        else
            DestroyLimbInBody(limb, entMan, protoMan, body);
    }

    private void DestroyLimbOutsideOfBody(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (!entMan.TryGetComponent<ConnectedOrganComponent>(limb, out var connectedOrgan) ||
            connectedOrgan.Organs == null)
            MakeNoiseAndDelete(limb, entMan);
        else
        {
            var connectedOrgans = new List<EntityUid>(connectedOrgan.Organs.ContainedEntities); // A copy because we're about to modify original
        
            DropAllConnected(connectedOrgans, entMan);
            MakeNoiseAndDelete(limb, entMan);
        }
    }

    private static void DestroyLimbInBody(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan, BodyComponent body)
    {
        var connectedOrgans = body.Organs?.ContainedEntities.Where(p =>
            entMan.TryGetComponent<OrganComponent>(p, out var organ) &&
            protoMan.Index(organ.Category)?.ConnectsTo == limb.Comp.Organ!.Category).ToList();

        DropAllConnected(connectedOrgans, entMan);
        MakeNoiseAndDelete(limb, entMan);
    }

    private static void DropAllConnected(List<EntityUid>? organs, IEntityManager entMan)
    {
        if (organs == null || organs.Count == 0) return;

        var container = entMan.System<SharedContainerSystem>();

        foreach (var organ in organs)
            container.TryRemoveFromContainer(organ);
    }

    private static void MakeNoiseAndDelete(EntityUid target, IEntityManager entMan)
    {
        entMan.System<SharedAudioSystem>().PlayPredicted(_gibSound, target, null);
        entMan.QueueDeleteEntity(target);
    }
}