using Content.Shared.FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Events;

[ByRefEvent]
public struct IsJobAllowedEvent(ICommonSession player, ProtoId<FactionPrototype> factionId, ProtoId<JobPrototype> jobId, bool cancelled = false)
{
    public readonly ICommonSession Player = player;
    // Far Horizons
    // we might need separate whitelists/requirements for factions in the future
    public readonly ProtoId<FactionPrototype> FactionId = factionId;
    public readonly ProtoId<JobPrototype> JobId = jobId;
    public bool Cancelled = cancelled;
}
