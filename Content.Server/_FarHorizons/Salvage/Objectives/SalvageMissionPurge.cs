using System.Linq;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FarHorizons.Salvage.Objectives;

public sealed partial class SalvageMissionPurge : BaseSalvageMissionObjectiveHandler
{
    private readonly List<EntProtoId> _purgeTargets = ["FolderSalvageMissionObjective"];
    private readonly double _inPocketChance = 0.3;
    static readonly List<string> _pocketSlots = ["pocket1", "pocket2"];
    static readonly List<EntProtoId> _stuffProtos = ["Pen", "LuxuryPen", "Lighter", "CheapLighter", "FlippoLighter", "CigPackBlue", "CigPackRed", "CigPackBlack", "Cigar", "CigarGold"];

    public override void AFterFTLToMap(EntityUid shuttle) => 
        Announce(GetAnnouncement());
    public override void BeforeFTLFromMap(EntityUid shuttle)
    {
        if (GetExpeditionConsole(shuttle) is not EntityUid expedConsole)
            return;
        
        var allTargets = GetAllMarkedEntities();
        var targetsDestroyed = GetNumSpawnables() - allTargets.Count();
        SetRewardComponent(expedConsole, ResolveCompletion(targetsDestroyed));
    }
    public override void BeforeFTLToMap(EntityUid shuttle){} // Override intentionally left empty

    public override void OnMapCreated()
    {
        var factions = IoCManager.Resolve<ISharedFactionManager>();
        var humanoid = EntMan.System<HumanoidAppearanceSystem>();
        var metadata = EntMan.System<MetaDataSystem>();
        var state = EntMan.System<MobStateSystem>();
        var damageable = EntMan.System<DamageableSystem>();
        var stationSpawning = EntMan.System<StationSpawningSystem>();
        var inventory = EntMan.System<InventorySystem>();

        int bodiesSpawned = 0;

        var possibleFactions = factions.ListPlayableFactions().Where(p => p.Major).ToList();
        var selectedFaction = possibleFactions[Rand.Next(possibleFactions.Count)];

        for (var i = 0; i < GetNumSpawnables(); i++)
        {
            if (GetRandomEmptyTileInDungeon() is not EntityCoordinates pos)
                return;

            var proto = _purgeTargets[Rand.Next(_purgeTargets.Count)];
            var spawned = SpawnAndMarkEntity(proto, pos);

            if (Rand.Prob(_inPocketChance))
            {
                var slot = _pocketSlots[Rand.Next(_pocketSlots.Count)];
                var damage = SalvageMissionRescue.RandomDamage(ProtoMan, Rand, 100, 200, 4);
                var body = SalvageMissionRescue.SpawnRandomBody(ProtoMan, EntMan, Rand, pos, humanoid, metadata, state, damageable, factions, stationSpawning, inventory, selectedFaction, true, damage, true);
                if (!inventory.TryEquip(body, spawned, slot, force: true))
                    EntMan.DeleteEntity(body);
                else{
                    bodiesSpawned++;
                }
            }
        }

        for (var i = 0; i < bodiesSpawned * 2; i++)
        {
            if (GetRandomEmptyTileInDungeon() is not EntityCoordinates pos)
                return;

            var slot = _pocketSlots[Rand.Next(_pocketSlots.Count)];
            var item = _stuffProtos[Rand.Next(_stuffProtos.Count)];

            var damage = SalvageMissionRescue.RandomDamage(ProtoMan, Rand, 100, 200, 4);
            var body = SalvageMissionRescue.SpawnRandomBody(ProtoMan, EntMan, Rand, pos, humanoid, metadata, state, damageable, factions, stationSpawning, inventory, selectedFaction, true, damage, true);
            var itemEnt = EntMan.SpawnAtPosition(item, pos);
            if (!inventory.TryEquip(body, itemEnt, slot, force: true))
                EntMan.DeleteEntity(itemEnt);
        }
    }

    private int GetNumSpawnables() => 
        Objective.NumTargets.GetValueOrDefault(Difficulty, 0) + Objective.BonusCap;
}