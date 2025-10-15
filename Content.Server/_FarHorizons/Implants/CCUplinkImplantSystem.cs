using System.Linq;
using System.Threading;
using Content.Server._FarHorizons.CCOperations;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles.Jobs;
using Content.Server.Store.Systems;
using Content.Shared.CCOperations.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._FarHorizons.Implants;

public sealed class CCUplinkSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly CCOperationsSystem _operations = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    // register user as special agent
    // somehow listen back to the events of CCUplinkUpdate{state, balanceDiff}
    // and also listen to event CCKillAgent {uid}

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CCUplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeNetworkEvent<CCAgentToggleUplinkMessage>(OnToggleAgentUplink);
        SubscribeNetworkEvent<CCNeutralizeAgentMessage>(OnNeutralizeAgent);
    }

    private void OnToggleAgentUplink(CCAgentToggleUplinkMessage message, EntitySessionEventArgs args)
    {
        var entityUid = new EntityUid(message.AgentId);
        if (_operations.IsKnownAgent(entityUid))
        {
            var agent = GetAgentData(entityUid);
            agent.UplinkOpen = message.UplinkStatus;
            var ev = new CCAgentUpdatedEvent(agent);
            RaiseLocalEvent(entityUid, ev, true);
        }
    }

    private void OnNeutralizeAgent(CCNeutralizeAgentMessage message, EntitySessionEventArgs args)
    {
        var entityUid = new EntityUid(message.AgentId);
        if (_operations.IsKnownAgent(entityUid))
        {
            var coords = _transformSystem.GetMapCoordinates(entityUid);
            Timer.Spawn(_gameTiming.TickPeriod,
                () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                    4, 1, 2, entityUid, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                CancellationToken.None);

            _bodySystem.GibBody(entityUid);
        }
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity == null)  // screw this event system tbh
            return;

        var entityId = args.Mind.OwnedEntity.Value;
        if (_operations.IsKnownAgent(entityId))  // and screw it even more
        // on spawn the entity doesn't have a proper mind, so we need to update it later
        // however, we can't directly check if target has an implant, and deep check wouldn't be sufficient
        // so we do it this dirty way
        {
            var agentData = GetAgentData(entityId);
            var ev = new CCAgentUpdatedEvent(agentData);
            RaiseLocalEvent(entityId, ev, true);
            
        }
    }

    public CCOperativeAgent GetAgentData(EntityUid uid)
    {
        var (entity, entityData) = _entityManager.GetEntityData(new NetEntity(uid.Id));
        var age = 0;
        var gender = "unknown";
        var species = "unknown";
        var name = entityData.EntityName;
        var state = "unknown";
        var job = "unknown";

        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            if (mind.CharacterName != null)
                name = mind.CharacterName;
            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                job = jobName;
        }
        if (_entityManager.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            age = appearance.Age;
            gender = appearance.Gender.ToString();
            species = appearance.Species;
        }
        if (_entityManager.TryGetComponent<MindComponent>(uid, out var mindComponent))
        {
            if (mindComponent.CharacterName != null)
                name = mindComponent.CharacterName;
        }

        return new CCOperativeAgent(uid.Id, false, name, age, job, species, gender);
    }

    private void OnImplantImplanted(EntityUid implantUid, CCUplinkImplantComponent component, ref ImplantImplantedEvent args)
    {
        var uid = args.Implanted;
        var agentData = GetAgentData(uid);
        var ev = new CCAgentInitializedEvent(agentData);
        RaiseLocalEvent(implantUid, ref ev);

        // if (TryComp<StoreComponent>(uid, out var store))
        // {
        //     if (store.Balance.ContainsKey("ActionPoint"))
        //     {
        //         var currency = new Dictionary<string, FixedPoint2> { ["ActionPoint"] = 3 };
        //         _storeSystem.TryAddCurrency(currency, uid, store);
        //     }
        // }

    }
}
