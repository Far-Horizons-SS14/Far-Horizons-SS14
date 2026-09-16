using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Radiation.Systems;
using Content.Shared.CCVar;
using Content.Shared.Radiation.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._FarHorizons.Radiation;

[TestFixture]
public sealed class RadiationSystemTest : GameTest
{
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.RadiationGridcastUpdateRate), 0.01f)]
    public async Task RequestedGridTileRadiationIsCached()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entMan = server.EntMan;
        var radiation = entMan.System<RadiationSystem>();
        var coordinates = new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 0.5f));

        await server.WaitAssertion(() =>
        {
            var source = entMan.SpawnEntity(null, coordinates);
            var sourceComponent = entMan.AddComponent<RadiationSourceComponent>(source);

            radiation.SetSourceEnabled((source, sourceComponent), false);
            radiation.SetIntensity((source, sourceComponent), 10f);
            radiation.SetSourceEnabled((source, sourceComponent), true);

            Assert.That(radiation.GetRadiationAtCoordinates(coordinates), Is.Zero);
            radiation.RequestTileRadiationSampling(coordinates);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(radiation.GetRadiationAtCoordinates(coordinates), Is.GreaterThan(0f));
        });
    }
}
