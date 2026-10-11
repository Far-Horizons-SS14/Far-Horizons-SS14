using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Station.Systems;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Movement.Systems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Tag;
using Robust.Server.Containers;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private TransformSystem _xformSys = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private ContainerSystem _container = default!;

    public override void Initialize()
    {
        InitializeActions();
        InitializeGizmo();
        InitializeConsole();
        InitializeOrgans();
        InitializeVest();
        InitializeExtractor();
        InitializeRoundEnd();
        base.Initialize();
    }
}
