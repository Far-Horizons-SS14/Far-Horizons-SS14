using Content.Server._FarHorizons.NPC;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUtilitySystem
{
    // Basically if TargetInLOSOrCurrentCon checks Opaque - mobs can see through windows, but can't see through crates/ore boxes/machines
    // Machines must have Opaque layer on them so you can target them with lasers
    // Therefore we switch to checking HighImpassable and then manually excluding all windows with this function
    private bool LineOfSightIgnoreCheck(EntityUid uid) => HasComp<LineOfSightExcludeComponent>(uid);
}