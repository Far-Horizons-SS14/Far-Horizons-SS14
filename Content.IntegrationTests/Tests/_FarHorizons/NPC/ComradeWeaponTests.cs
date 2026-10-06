using Content.IntegrationTests.Fixtures;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._FarHorizons.NPC;

[TestFixture]
public sealed class ComradeWeaponTests : GameTest
{
    [TestPrototypes]
    private const string ReloadPrototypes = """
        - type: entity
          id: TestMagazinePistolLoaded
          parent: MagazinePistolSP
          components:
          - type: BallisticAmmoProvider
            unspawnedCount: 10

        - type: entity
          id: TestMagazineLightRifleLoaded
          parent: MagazineLightRifleSP
          components:
          - type: BallisticAmmoProvider
            unspawnedCount: 30

        - type: startingGear
          id: TestSovietComradeGearSawnEmpty
          equipment:
            belt: ClothingSovietBelt
          storage:
            belt:
            - ShellShotgun
          inhand:
          - WeaponShotgunSawnEmpty

        - type: startingGear
          id: TestSovietComradeGearMakarovEmpty
          equipment:
            belt: ClothingSovietBelt
          storage:
            belt:
            - TestMagazinePistolLoaded
          inhand:
          - WeaponPistolMakarov

        - type: startingGear
          id: TestSovietComradeGearAkEmpty
          equipment:
            belt: ClothingSovietBelt
          storage:
            belt:
            - TestMagazineLightRifleLoaded
          inhand:
          - WeaponRifleAk

        - type: entity
          id: TestMobSovietComradeSawnEmpty
          parent: MobSovietComrade
          components:
          - type: Loadout
            prototypes:
            - TestSovietComradeGearSawnEmpty

        - type: entity
          id: TestMobSovietComradeMakarovEmpty
          parent: MobSovietComrade
          components:
          - type: Loadout
            prototypes:
            - TestSovietComradeGearMakarovEmpty

        - type: entity
          id: TestMobSovietComradeAkEmpty
          parent: MobSovietComrade
          components:
          - type: Loadout
            prototypes:
            - TestSovietComradeGearAkEmpty
        """;

    [TestCase("TestMobSovietComradeSawnEmpty")]
    [TestCase("TestMobSovietComradeMakarovEmpty")]
    [TestCase("TestMobSovietComradeAkEmpty")]
    public async Task ComradeReloadsEmptyWeapon(string prototype)
    {
        var map = await Pair.CreateTestMap();
        var gunSystem = SEntMan.System<SharedGunSystem>();
        var slotsSystem = SEntMan.System<ItemSlotsSystem>();
        EntityUid comrade = default;
        EntityUid gunUid = default;

        await Server.WaitAssertion(() =>
        {
            comrade = SEntMan.SpawnEntity(prototype, map.GridCoords);

            Assert.That(gunSystem.TryGetGun(comrade, out var gun), Is.True);
            gunUid = gun.Owner;
            EmptyGun(gunUid, slotsSystem);
            Assert.That(gunSystem.GetAmmoCount(gunUid), Is.Zero);

        });

        await Pair.RunSeconds(3f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(gunUid), Is.True);
            Assert.That(gunSystem.TryGetGun(comrade, out var equippedGun), Is.True);
            Assert.That(equippedGun.Owner, Is.EqualTo(gunUid));
            Assert.That(gunSystem.GetAmmoCount(gunUid), Is.GreaterThan(0));
        });
    }

    private void EmptyGun(EntityUid gun, ItemSlotsSystem slotsSystem)
    {
        if (!SEntMan.TryGetComponent<ItemSlotsComponent>(gun, out var itemSlots))
            return;

        if (slotsSystem.TryGetSlot(gun, SharedGunSystem.MagazineSlot, out var magazineSlot, itemSlots) &&
            magazineSlot.Item is { } magazine)
        {
            SEntMan.RemoveComponent<BallisticAmmoProviderComponent>(magazine);
            SEntMan.AddComponent<BallisticAmmoProviderComponent>(magazine);
        }

        if (slotsSystem.TryGetSlot(gun, SharedGunSystem.ChamberSlot, out var chamberSlot, itemSlots) &&
            chamberSlot.Item is { } chamber)
        {
            Assert.That(slotsSystem.TryEject(gun, chamberSlot, null, out _), Is.True);
            SEntMan.DeleteEntity(chamber);
        }
    }
}
