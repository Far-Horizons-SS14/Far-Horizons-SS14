using Content.Server._FarHorizons.Factions;
using Content.Server.Hands.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.Storage.EntitySystems;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Access.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage;
using Robust.Server.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing.Systems;

public sealed partial class OutfitSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private HandsSystem _handSystem = default!;
    [Dependency] private InventorySystem _invSystem = default!;
    [Dependency] private SharedStationSpawningSystem _spawningSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private StorageSystem _storageSystem = default!;
    [Dependency] private IServerFactionManager _factions = default!; // Far Horizons
    [Dependency] private ContainerSystem _container = default!; // Far Horizons

    public bool SetOutfit(EntityUid target, string gear, Action<EntityUid, EntityUid>? onEquipped = null, bool unremovable = false)
    {
        if (!TryComp(target, out InventoryComponent? inventoryComponent))
            return false;

        if (!_prototypeManager.TryIndex<StartingGearPrototype>(gear, out var startingGear))
            return false;

        #region Starlight
        HumanoidCharacterProfile? profile = null;
        if (TryComp<HumanoidCharacterProfileComponent>(target, out var profileComp))
            profile = profileComp.Profile;
        #endregion Starlight

        var spawnCoords = Comp<TransformComponent>(target).Coordinates; // Far Horizons
        ICommonSession? session = null;

        if (_invSystem.TryGetSlots(target, out var slots))
        {
            foreach (var slot in slots)
            {
                _invSystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);
                var gearStr = ((IEquipmentLoadout) startingGear).GetGear(slot.Name);
                if (gearStr == string.Empty)
                    continue;

                var equipmentEntity = Spawn(gearStr, spawnCoords); // Far Horizons
                if (slot.Name == "id" &&
                    TryComp(equipmentEntity, out PdaComponent? pdaComponent) &&
                    TryComp<IdCardComponent>(pdaComponent.ContainedId, out var id))
                {
                    id.FullName = Comp<MetaDataComponent>(target).EntityName;
                }

                _invSystem.TryEquip(target, equipmentEntity, slot.Name, silent: true, force: true, inventory: inventoryComponent);
                if (unremovable)
                    EnsureComp<UnremoveableComponent>(equipmentEntity);

                onEquipped?.Invoke(target, equipmentEntity);
            }
        }

        var coords = Transform(target).Coordinates;
        foreach (var (slotName, storageContainers) in startingGear.Storage)
        {
            if (storageContainers.Count == 0)
                continue;

            if (!_invSystem.TryGetSlotEntity(target, slotName, out var slotEnt))
                continue;

            if (TryComp<StorageComponent>(slotEnt, out var storage))
            {
                foreach (var entProto in storageContainers)
                {
                    var spawnedEntity = SpawnAtPosition(entProto, coords);
                    _storageSystem.Insert(slotEnt.Value, spawnedEntity, out _, user: null, storageComp: storage, playSound: false);
                }
            }
            else if (TryComp<ItemSlotsComponent>(slotEnt, out var itemSlots))
            {
                foreach (var entProto in storageContainers)
                {
                    var spawnedEntity = SpawnAtPosition(entProto, coords);
                    _itemSlotsSystem.TryInsertEmpty((slotEnt.Value, itemSlots), spawnedEntity, null, excludeUserAudio: true);
                }
            }
        }

        if (TryComp(target, out HandsComponent? handsComponent))
        {
            foreach (var prototype in startingGear.Inhand)
            {
                var inhandEntity = Spawn(prototype, coords);
                _handSystem.TryPickup(target, inhandEntity, checkActionBlocker: false, handsComp: handsComponent);
            }
        }

        // See if this starting gear is associated with a job
        // Far horizons, find outfits from faction jobs
        foreach (var jobAssignment in _factions.ListFactionJobs())
        {
            if (!_prototypeManager.TryIndex(jobAssignment.Job, out var job) ||
                !_prototypeManager.TryIndex(jobAssignment.Faction, out var faction))
                continue;

            if (job.StartingGear != gear)
                continue;

            var jobProtoId = _factions.OverrideJobLoadout(jobAssignment); // Far Horizons
            if (!_prototypeManager.TryIndex<RoleLoadoutPrototype>(jobProtoId, out var jobProto))
                break;


            // Don't require a player, so this works on Urists
            profile ??= TryComp<HumanoidProfileComponent>(target, out var comp)
                ? HumanoidCharacterProfile.DefaultWithSpecies(comp.Species, comp.Sex)
                : new HumanoidCharacterProfile();
            // Try to get the user's existing loadout for the role
            profile.Loadouts.TryGetValue(jobProtoId, out var roleLoadout);

            if (roleLoadout == null)
            {
                #region Starlight
                if (TryComp<ActorComponent>(target, out var actor))
                    session = actor.PlayerSession;
                #endregion
                // If they don't have a loadout for the role, make a default one
                roleLoadout = new RoleLoadout(jobProtoId);
                roleLoadout.SetDefault(profile, session, _prototypeManager);
            }

            // Equip the target with the job loadout
            _spawningSystem.EquipRoleLoadout(target, roleLoadout, jobProto);
        }

        return true;
    }
}
