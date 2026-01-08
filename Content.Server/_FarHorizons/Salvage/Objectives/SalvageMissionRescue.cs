
using System.Linq;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Microsoft.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FarHorizons.Salvage.Objectives;

public sealed partial class SalvageMissionRescue : BaseSalvageMissionObjectiveHandler
{
    const int DecoyBodies = 20;
    const int TotalDamageForBonus = 100;
    static readonly EntProtoId GasMask = "ClothingMaskGas";
    const string MaskSlot = "mask";

    public override void AFterFTLToMap(EntityUid shuttle)
    {
        var targets = GetAllMarkedEntities();
        List<string> names = [];
        foreach (var uid in targets)
        {
            if(!EntMan.TryGetComponent<MetaDataComponent>(uid, out var metadata))
                continue;

            names.Add(metadata.EntityName);
        }

        if(names.Count == 1)
        {
            Announce(Loc.GetString(Objective.Announcement, ("names", names[0])));
            return;
        }

        var namesString = $"{string.Join("; ", names[..^1])} and {names[^1]}";
        Announce(Loc.GetString(Objective.Announcement, ("names", namesString)));
    }
    public override void BeforeFTLFromMap(EntityUid shuttle)
    {
        if (GetExpeditionConsole(shuttle) is not EntityUid expedConsole)
            return;

        var allTargets = GetAllMarkedEntitiesOnShuttle(shuttle);
        var numBonus = 0;
        foreach (var uid in allTargets)
            if(EntMan.TryGetComponent<DamageableComponent>(uid, out var damage) &&
               damage.TotalDamage <= TotalDamageForBonus)
                numBonus++;
        numBonus = Math.Min(numBonus, Objective.BonusCap);

        var completion = (allTargets.Count >= Objective.NumTargets.GetValueOrDefault(Difficulty, 0),
                          numBonus,
                          Objective.BonusCap,
                          allTargets.Count >= Objective.NumTargets.GetValueOrDefault(Difficulty, 0) ?
                            Objective.BaseReward.GetValueOrDefault(Difficulty, 0) + Objective.Bonus * numBonus :
                            0);
        SetRewardComponent(expedConsole, completion);
        DeleteWithEffect(allTargets);

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

        int objectivesSpawned = 0;

        var possibleFactions = factions.ListPlayableFactions().Where(p => p.Major).ToList();
        var selectedFaction = possibleFactions[Rand.Next(possibleFactions.Count)];

        for (var i = 0; i < GetNumSpawnables(); i++)
        {
            if (GetRandomEmptyTileInDungeon() is not EntityCoordinates pos)
                return;

            var damage = RandomDamage(ProtoMan, Rand, 100, 200, 4);
            var body = SpawnRandomBody(ProtoMan, EntMan, Rand, pos, humanoid, metadata, state, damageable, factions, stationSpawning, inventory, selectedFaction, true, damage, true);

            if (Rand.Prob(0.4))
            {
                var gasMaskEnt = EntMan.SpawnAtPosition(GasMask, pos);
                if (!inventory.TryEquip(body, gasMaskEnt, MaskSlot, force: true))
                    EntMan.DeleteEntity(gasMaskEnt);
            }

            if (objectivesSpawned < Objective.NumTargets.GetValueOrDefault(Difficulty, 0))
            {
                MarkEntity(body);
                objectivesSpawned++;
            }
        }
    }

    public static EntityUid SpawnRandomBody(
        IPrototypeManager ProtoMan,
        IEntityManager EntMan,
        Random Rand,
        EntityCoordinates pos, 
        HumanoidAppearanceSystem humanoidAppearance, 
        MetaDataSystem metadata,
        MobStateSystem? state = null,
        DamageableSystem? damageable = null,
        ISharedFactionManager? factions = null,
        StationSpawningSystem? stationSpawning = null,
        InventorySystem? inventory = null,
        FactionPrototype? faction = null,
        bool dead = true,
        DamageSpecifier? damage = null,
        bool randomLoadout = true)
    {
        var character = HumanoidCharacterProfile.Random();
        var species = ProtoMan.Index(character.Species);

        var ent = EntMan.SpawnAtPosition(species.Prototype, pos);
        humanoidAppearance.LoadProfile(ent, character);
        metadata.SetEntityName(ent, character.Name);

        if (dead && state != null)
            state.ChangeMobState(ent, MobState.Dead);
        
        if (damage != null && damageable != null)
            damageable.TryChangeDamage(ent, damage);
        
        if (randomLoadout && faction != null && factions != null && stationSpawning != null)
        {
            var jobs = factions.ListFactionJobs().Where(p => p.Faction == faction ).ToList();
            var valid = false;

            RoleLoadout loadout;
            RoleLoadoutPrototype loadoutProto;

            do
            {
                var job = jobs[Rand.Next(jobs.Count)];
                var jobProto = ProtoMan.Index(job.Job);

                valid = string.IsNullOrEmpty(jobProto.JobEntity);

                var loadoutProtoId = factions.OverrideJobLoadout(job);
                loadoutProto = ProtoMan.Index(loadoutProtoId);
                loadout = new RoleLoadout(loadoutProtoId);
            } while (!valid);

            loadout.SetDefault(character, null, ProtoMan);

            stationSpawning.EquipRoleLoadout(ent, loadout, loadoutProto);

            if (inventory != null)
                if (inventory.TryUnequip(ent, "id", out var id, true, true))
                    EntMan.DeleteEntity(id);
        }

        return ent;
    }

    public static DamageSpecifier RandomDamage(IPrototypeManager ProtoMan, Random Rand, int minDamage, int maxDamage, int maxDamageTypes)
    {
        var damageTypes = ProtoMan.EnumeratePrototypes<DamageTypePrototype>().ToList();

        var damage = new DamageSpecifier();
        float chance = 1;
        for (var i = 0; i < maxDamageTypes; i++)
        {
            if(Rand.Prob(chance))
            {
                var type = damageTypes[Rand.Next(damageTypes.Count)].ID;
                if (damage.DamageDict.ContainsKey(type))
                    continue;

                damage.DamageDict.Add(type, Rand.Next(minDamage, maxDamage));       
            }
            chance -= 1 / maxDamageTypes;
        }

        return damage;
    }

    private int GetNumSpawnables() => 
        Objective.NumTargets.GetValueOrDefault(Difficulty, 0) + DecoyBodies;
}