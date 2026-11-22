using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.LimbDamage;

public sealed partial class LimbTargettingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeLimbTargetMessage>(OnChangeTargetRequest);
    }

    private void OnChangeTargetRequest(ChangeLimbTargetMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user) return;

        UpdateTarget(user, ev.Target);
    }

    public void UpdateTarget(Entity<LimbTargettingComponent?> ent, ProtoId<OrganCategoryPrototype> target)
    {
        if (!Resolve(ent, ref ent.Comp)) return;

        ent.Comp.Target = target;
        Dirty(ent);
    }
}