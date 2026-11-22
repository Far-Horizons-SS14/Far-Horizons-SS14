using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryProgressStatusChain : ISurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<LimbStatusChainPrototype> Chain;

    [DataField] public bool NeedsToBeAlive = true;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (NeedsToBeAlive &&
            !entMan.System<MobStateSystem>().IsAlive(target))
            return;

        var targetEnt = limb ?? target;
        var steps = IoCManager.Resolve<IPrototypeManager>().Index(Chain).Steps;

        var current = -1;
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (!limbStatus.HasStatus(targetEnt, steps[i]))
                continue;

            current = i;
            break;
        }

        if (current == steps.Count - 1)
            return;

        if (current >= 0)
            limbStatus.RemoveStatusEffect(targetEnt, steps[current]);

        limbStatus.SetStatusEffect(targetEnt, steps[current + 1]);
    }
}
