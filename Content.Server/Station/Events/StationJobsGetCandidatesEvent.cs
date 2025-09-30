using Content.Shared.FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Events;

// Far Horizons - factions
[ByRefEvent]
public readonly record struct StationJobsGetCandidatesEvent(NetUserId Player, List<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job)> Jobs);
