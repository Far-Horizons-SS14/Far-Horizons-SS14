using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Shared.CCOperations.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Implants;

public sealed class CCUplinkSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CCUplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(EntityUid uid, CCUplinkImplantComponent component, ref ImplantImplantedEvent args)
    {
        
        if (args.Implanted == null)
            return;

        var ev = new CCAgentInitializedEvent(args.Implanted.Value);
        RaiseLocalEvent(uid, ref ev);
        
        // register user as special agent
        // somehow listen back to the events of CCUplinkUpdate{state, balanceDiff}
        // and also listen to event CCKillAgent {uid}

        if (TryComp<StoreComponent>(uid, out var store))
        {
            if (store.Balance.ContainsKey("ActionPoint"))
            {
                var currency = new Dictionary<string, FixedPoint2> { ["ActionPoint"] = 3 };
                _storeSystem.TryAddCurrency(currency, uid, store);
            }
        }

    }
}
