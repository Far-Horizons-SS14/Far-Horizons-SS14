using Content.Shared._FarHorizons.Factions;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

public abstract partial class SharedIdCardSystem
{
    public bool TryChangeFaction(Entity<IdCardComponent?> ent, ProtoId<FactionPrototype>? faction)
    {
        if (!Resolve(ent, ref ent.Comp)) return false;
        ent.Comp.Faction = faction;
        return true;
    }
}