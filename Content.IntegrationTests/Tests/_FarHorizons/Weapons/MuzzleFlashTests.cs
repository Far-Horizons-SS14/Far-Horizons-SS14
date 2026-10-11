using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._FarHorizons.Weapons;

public sealed class MuzzleFlashTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task MuzzleFlashDoesNotDrainMKTFlashlight()
    {
        var mkT = await PlaceInHands("WeaponPistolMKT");
        var handheldLight = SEntMan.System<SharedHandheldLightSystem>();

        await Server.WaitPost(() =>
            handheldLight.SetActivated(ToServer(mkT), true, SEntMan.GetComponent<HandheldLightComponent>(ToServer(mkT))));
        await RunTicks(5);

        await Client.WaitPost(() =>
            Assert.That(CEntMan.GetComponent<PointLightComponent>(CEntMan.GetEntity(mkT)).Energy, Is.GreaterThan(0f)));

        await Client.WaitPost(() =>
            CEntMan.EventBus.RaiseLocalEvent(
                CEntMan.GetEntity(mkT),
                new MuzzleFlashEvent(mkT, "MuzzleFlashEffect", Angle.Zero),
                broadcast: true));
        await Pair.RunSeconds(1f);

        await Client.WaitPost(() =>
            Assert.That(CEntMan.GetComponent<PointLightComponent>(CEntMan.GetEntity(mkT)).Energy, Is.GreaterThan(0f),
                "The MK-T flashlight should retain its energy after the muzzle flash animation ends."));
    }
}
