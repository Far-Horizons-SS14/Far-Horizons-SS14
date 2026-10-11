using Content.Shared._FarHorizons.Factions;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public abstract partial class SharedRoleSystem
{
    public ProtoId<FactionPrototype>? MindGetFaction(Entity<MindRoleComponent?> mind) => 
        !Resolve(mind, ref mind.Comp) ? null : mind.Comp.FactionPrototype;
}