using System.Numerics;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.Airlocks;

public sealed class AirlockFixupSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, MapInitEvent>(OnAirlockMapInit);
    }

    private void OnAirlockMapInit(EntityUid uid, AirlockComponent component, ref MapInitEvent args)
    {
        try
        {
            if (_entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID != "FHAirlock")
                return;
            
            var transform = _entityManager.GetComponent<TransformComponent>(uid);
            var localPosition = transform.LocalPosition;
            var nearestStructures = _entityManager.System<EntityLookupSystem>()
                .GetEntitiesInRange(uid, 0.5f, LookupFlags.Static);
            int neighbours = 0;
            foreach (var entity in nearestStructures)
            {
                if (_entityManager.HasComponent<PhysicsComponent>(entity))
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
