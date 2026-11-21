using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingUniqueIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ChangelingDevouredEvent>(OnChangelingDevoured);

        // Far Horizons start
        SubscribeLocalEvent<AbsorbConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbGetProgress);
        SubscribeLocalEvent<StealDNAConditionComponent, ObjectiveGetProgressEvent>(OnStealDNAGetProgress);
        // Far Horizons end
    }

    private void OnChangelingDevoured(ref ChangelingDevouredEvent args)
    {
        if (!args.Unique)
            return;

        if (!_mind.TryGetObjectiveComp<ChangelingUniqueIdentityConditionComponent>(args.Changeling, out var obj))
            return;

        obj.UniqueIdentities++;
    }

    private void OnGetProgress(Entity<ChangelingUniqueIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp, _number.GetTarget(ent));
    }

    private float GetProgress(ChangelingUniqueIdentityConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (comp.UniqueIdentities >= target)
            return 1f;

        return (float)comp.UniqueIdentities / (float)target;
    }

    // Far Horizons start
    private void OnAbsorbGetProgress(Entity<AbsorbConditionComponent> absorbCondition, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(absorbCondition);
        if (target != 0)
            args.Progress = MathF.Min(absorbCondition.Comp.Absorbed / target, 1f);
        else args.Progress = 1f;
    }

    private void OnStealDNAGetProgress(Entity<StealDNAConditionComponent> stealDNACondition, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(stealDNACondition);
        if (target != 0)
            args.Progress = MathF.Min(stealDNACondition.Comp.DNAStolen / target, 1f);
        else args.Progress = 1f;
    }
    // Far Horizons end
}
