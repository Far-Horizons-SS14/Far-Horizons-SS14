using Content.Server.Construction.Components;
using Content.Shared._FarHorizons.Doors.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Airlocks;

public sealed class AirlockFixupSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FHDoorComponent, MapInitEvent>(OnAirlockMapInit);
    }

    private void OnAirlockMapInit(EntityUid uid, FHDoorComponent component, ref MapInitEvent args)
    {
        try
        {
            var transform = _entityManager.GetComponent<TransformComponent>(uid);
            var localPosition = transform.LocalPosition;
            var nearestStructures = _entityManager.System<EntityLookupSystem>()
                .GetEntitiesInRange(uid, 0.5f, LookupFlags.Static);
            int neighbours = 0;
            foreach (var entity in nearestStructures)
            {
                if (_entityManager.HasComponent<PhysicsComponent>(entity) || _entityManager.HasComponent<FHDoorComponent>(entity) || _entityManager.HasComponent<ConstructionComponent>(entity))
                {
                    var neighbourTransform = _entityManager.GetComponent<TransformComponent>(entity);
                    var neighbourLocalPosition = neighbourTransform.LocalPosition;
                    if (Math.Abs(neighbourLocalPosition.X - localPosition.X) < 0.01)  // never trust floating point
                    {
                        neighbours++;
                    }
                }
            }

            if (neighbours > 1) // i am loosing my sanity
                transform.LocalRotation = Angle.FromDegrees(90);
            else
                transform.LocalRotation = 0;

        }
        catch (Exception e)
        {
            return;
        }

    }
}
