using Content.Client.Lobby;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Lobby;

//Starlight, I diverged this file since our character profile system is different. Not a fan but whatever

[TestFixture]
[TestOf(typeof(ClientPreferencesManager))]
[TestOf(typeof(ServerPreferencesManager))]
public sealed class CharacterCreationTest
{
    [Test]
    public async Task CreateDeleteCreateTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var server = pair.Server;
        var client = pair.Client;
        var user = pair.Client.User!.Value;
        var clientPrefManager = client.Resolve<IClientPreferencesManager>();
        var serverPrefManager = server.Resolve<IServerPreferencesManager>();

        Assert.That(client.Resolve<IStateManager>().CurrentState, Is.TypeOf<LobbyState>());
        await pair.RunTicksSync(5);

        var clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(1));

        HumanoidCharacterProfile profile = null;
        await client.WaitPost(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        clientCharacters[1].AssertEquals(profile);

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);

        var serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        clientCharacters[1].AssertEquals(profile);

        await client.WaitAssertion(() => clientPrefManager.DeleteCharacter(1));
        await pair.RunTicksSync(5);
        Assert.That(clientPrefManager.Preferences?.Characters.Count, Is.EqualTo(1));
        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 1, maxTicks: 60);
        Assert.That(serverPrefManager.GetPreferences(user).Characters.Count, Is.EqualTo(1));

        await client.WaitIdleAsync();

        await client.WaitAssertion(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        clientCharacters[1].AssertEquals(profile);

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);
        serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        clientCharacters[1].AssertEquals(profile);
        await pair.CleanReturnAsync();
    }
}
