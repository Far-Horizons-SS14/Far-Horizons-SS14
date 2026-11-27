using System.Collections.Generic;
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
    [TestCase(1)]
    [TestCase(300)]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.RadiationGridcastUpdateRate), 0.01f)]
    public async Task RequestedGridTileRadiationIsCached(int sourceCount)
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entMan = server.EntMan;
        var radiation = entMan.System<RadiationSystem>();
        var coordinates = new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 0.5f));
        var sources = new List<EntityUid>();

        await server.WaitAssertion(() =>
        {
            for (var i = 0; i < sourceCount; i++)
            {
                var source = entMan.SpawnEntity(null, coordinates);
                var sourceComponent = entMan.AddComponent<RadiationSourceComponent>(source);

                radiation.SetSourceEnabled((source, sourceComponent), false);
                radiation.SetIntensity((source, sourceComponent), 10f);
                radiation.SetSourceEnabled((source, sourceComponent), true);
                sources.Add(source);
            }

            Assert.That(radiation.GetRadiationAtCoordinates(coordinates), Is.Zero);
            radiation.RequestTileRadiationSampling(coordinates);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(radiation.GetRadiationAtCoordinates(coordinates), Is.EqualTo(10f * sourceCount).Within(0.01f));

            foreach (var source in sources)
                radiation.SetSourceEnabled(source, false);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(radiation.GetRadiationAtCoordinates(coordinates), Is.Zero);
        });
    }
}
