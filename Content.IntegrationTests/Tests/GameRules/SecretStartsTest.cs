using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class SecretStartsTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings
    {
        Dirty = true,
        // Far Horizons start - Because we need players for secret to start, we need to add players to the pool
        DummyTicker = false,
        Connected = true,
        InLobby = true,
        // Far Horizons end
    };

    /// <summary>
    ///     Tests that when secret is started, all of the game rules it successfully adds are also started.
    /// </summary>
    [Test]
    public async Task TestSecretStarts()
    {
        var pair = Pair;

        var server = pair.Server;
        await server.WaitIdleAsync();
        var entMan = server.ResolveDependency<IEntityManager>();
        var gameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();

        // Far Horizons start - add players so secret can start.
        await server.AddDummySessions(4);
        await pair.RunUntilSynced();
        await server.WaitPost(() => gameTicker.ToggleReadyAll(true));
        // Far Horizons end

        await server.WaitAssertion(() =>
        {
            // this mimics roundflow:
            // rules added, then round starts
            gameTicker.AddGameRule("Secret");
            gameTicker.StartGamePresetRules();
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            Assert.That(gameTicker.GetAddedGameRules().Count(), Is.GreaterThan(1), $"No additional rules started by secret rule.");

            // End all rules
            gameTicker.ClearGameRules();
        });
    }
}
