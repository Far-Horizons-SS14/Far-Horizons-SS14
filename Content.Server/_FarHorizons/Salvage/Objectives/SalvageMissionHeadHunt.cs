using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Salvage.Objectives;

public sealed partial class SalvageMissionHeadHunt : BaseSalvageMissionObjectiveHandler
{
    static readonly ProtoId<OrganCategoryPrototype> _headOrganCategory = "Head";

    public override void AFterFTLToMap(EntityUid shuttle) => 
        Announce(GetAnnouncement());
    public override void BeforeFTLFromMap(EntityUid shuttle)
    {
        if (GetExpeditionConsole(shuttle) is not EntityUid expedConsole)
            return;
        
        var allTargets = GetAllMarkedEntitiesOnShuttle(shuttle);
        SetRewardComponent(expedConsole, ResolveCompletion(allTargets.Count));
        DeleteHeadOrBodyWithEffect(allTargets);
    }
    public override void BeforeFTLToMap(EntityUid shuttle){} // Override intentionally left empty

    public override void OnMapCreated()
    {
        foreach(var mob in GetAllSpawnedMobs())
            MarkHead(mob);
    }

    private void MarkHead(EntityUid mob)
    {
        if (!EntMan.TryGetComponent<BodyComponent>(mob, out var body) ||
            body.Organs == null ||
            body.Organs.ContainedEntities.Count == 0)
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!EntMan.TryGetComponent<OrganComponent>(organ, out var organComp) ||
                organComp.Category != _headOrganCategory)
                continue;

            MarkEntity(organ);
        }
    }

    private void DeleteHeadOrBodyWithEffect(HashSet<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            if (!EntMan.TryGetComponent<OrganComponent>(entity, out var organ) ||
                organ.Body == null)
            {
                DeleteWithEffect(entity);
                continue;
            }

            DeleteWithEffect(organ.Body.Value);
        }
    }
}