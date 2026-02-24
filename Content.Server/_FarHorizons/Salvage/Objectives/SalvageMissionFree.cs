using System.Linq;
using Content.Server.Body;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;

namespace Content.Server._FarHorizons.Salvage.Objectives;

public sealed partial class SalvageMissionFree : BaseSalvageMissionObjectiveHandler
{

    public override void AFterFTLToMap(EntityUid shuttle) => 
        Announce(GetAnnouncement());

    public override void BeforeFTLFromMap(EntityUid shuttle){} // Override intentionally left empty
    public override void BeforeFTLToMap(EntityUid shuttle){} // Override intentionally left empty

    public override void OnMapCreated()
    {
        var factions = IoCManager.Resolve<ISharedFactionManager>();
        var visualBody = EntMan.System<VisualBodySystem>();
        var profile = EntMan.System<HumanoidProfileSystem>();
        var metadata = EntMan.System<MetaDataSystem>();
        var state = EntMan.System<MobStateSystem>();
        var damageable = EntMan.System<DamageableSystem>();
        var stationSpawning = EntMan.System<StationSpawningSystem>();
        var inventory = EntMan.System<InventorySystem>();

        var possibleFactions = factions.ListPlayableFactions().Where(p => p.Major).ToList();
        var selectedFaction = possibleFactions[Rand.Next(possibleFactions.Count)];

        for (var i = 0; i < GetNumSpawnables(); i++)
        {
            if (GetRandomEmptyTileInDungeon() is not { } pos)
                return;

            var damage = SalvageMissionRescue.RandomDamage(ProtoMan, Rand, 100, 200, 4);
            SalvageMissionRescue.SpawnRandomBody(ProtoMan, EntMan, Rand, pos, visualBody, profile, metadata, state, damageable, factions, stationSpawning, inventory, selectedFaction, true, damage, true);
        }
    }

    private int GetNumSpawnables() => 
        Objective.NumTargets.GetValueOrDefault(Difficulty, 0);
}