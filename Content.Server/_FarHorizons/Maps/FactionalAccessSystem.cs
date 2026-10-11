using System.Linq;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Maps.FactionalAccess;

public sealed partial class FactionalAccessSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionalAccessComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<FactionalAccessComponent> ent, ref StationPostInitEvent args)
    {
        HashSet<EntityUid> grids = [];

        if (TryComp<StationDataComponent>(args.Station, out var stationData))
            grids = stationData.Grids;

        if (TryComp<StationCentcommComponent>(args.Station, out var centComp) && centComp.Entity != null)
            grids.Add(centComp.Entity.Value);

        if(grids.Count == 0 )
            return;

        foreach (var grid in grids)
        {
            ReplaceAccess(grid, ent.Comp);
        }
    }

    private void ReplaceAccess(EntityUid grid, FactionalAccessComponent faComp)
    {
        var readers = _lookup.GetEntitiesIntersecting(grid, LookupFlags.Uncontained | LookupFlags.Static);

        foreach (var uid in readers)
        {
            if (!TryComp<AccessReaderComponent>(uid, out var accessComp))
                continue;

            var oldAccessList = accessComp.AccessLists
                .Select(set => new HashSet<ProtoId<AccessLevelPrototype>>(set))
                .ToList();

            if (oldAccessList.Count == 0)
                continue;

            var newAccessList = new List<HashSet<ProtoId<AccessLevelPrototype>>>();

            foreach (var accessSet in oldAccessList)
            {
                var newSet = new HashSet<ProtoId<AccessLevelPrototype>>();

                foreach (var access in accessSet)
                {
                    newSet.Add(faComp.EquivalentAccessList.TryGetValue(access, out var equivalent)
                        ? equivalent
                        : access);
                }

                newAccessList.Add(newSet);
            }

            _access.TryRemoveAccesses((uid, accessComp), oldAccessList);
            _access.TryAddAccesses((uid, accessComp), newAccessList);
        }
    }
}