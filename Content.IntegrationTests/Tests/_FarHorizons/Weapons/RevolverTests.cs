using Content.Client.Weapons.Ranged.Components;
using Content.IntegrationTests.Tests.Helpers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Weapons.Ranged.Components;
using ClientGunSystem = Content.Client.Weapons.Ranged.Systems.GunSystem;
using Robust.Client.UserInterface;

namespace Content.IntegrationTests.Tests._FarHorizons.Weapons;

public sealed class RevolverTests : InteractionTest
{
    [Test]
    public async Task SpeedloaderReloadRaisesClientAmmoCounterUpdate()
    {
        var revolver = await Spawn("WeaponRevolverInspector");
        var speedloader = await Spawn("SpeedLoaderMagnumSP");
        var serverRevolver = ToServer(revolver);
        var clientRevolver = ToClient(revolver);
        var revolverComp = SEntMan.GetComponent<RevolverAmmoProviderComponent>(serverRevolver);
        var listener = CEntMan.System<AmmoCounterEventListenerSystem>();

        await Server.WaitPost(() => SGun.EmptyRevolver((serverRevolver, revolverComp)));
        await RunTicks(5);

        await Client.WaitPost(() =>
        {
            var clientRevolverComp = CEntMan.GetComponent<RevolverAmmoProviderComponent>(clientRevolver);
            Assert.That(clientRevolverComp.AmmoSlots, Is.All.Null);
            Assert.That(clientRevolverComp.Chambers, Is.All.Null);
            CEntMan.AddComponent<TestListenerComponent>(clientRevolver);
            CEntMan.GetComponent<AmmoCounterComponent>(clientRevolver).Control = new Control();
        });
        await Client.WaitPost(() => listener.Clear(clientRevolver));

        await Server.WaitPost(() =>
        {
            Assert.That(SGun.TryRevolverInsert(
                (serverRevolver, revolverComp),
                ToServer(speedloader),
                null), Is.True);
        });
        await RunTicks(5);

        await Client.WaitPost(() =>
        {
            var clientRevolverComp = CEntMan.GetComponent<RevolverAmmoProviderComponent>(clientRevolver);
            Assert.That(clientRevolverComp.AmmoSlots, Has.Some.Not.Null);
            Assert.That(clientRevolverComp.Chambers, Is.All.True,
                "Speedloader-loaded cartridges should be shown as unfired.");
        });

        Assert.That(listener.Count(clientRevolver), Is.EqualTo(1),
            "Replicated speedloader ammunition should raise UpdateAmmoCounterEvent on the client.");
    }
}

public sealed class AmmoCounterEventListenerSystem : TestListenerSystem<ClientGunSystem.UpdateAmmoCounterEvent>;
