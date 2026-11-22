using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Client.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;
using Content.Shared._FarHorizons.Magic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._FarHorizons.Store;

[TestFixture]
public sealed class StoreAccessTest : InteractionTest
{
    [Test]
    public async Task FirstUplinkImplantCreatesAndOpensItsStore()
    {
        var recipient = SPlayer;
        NetEntity implantNet = default;

        await Server.WaitPost(() =>
        {
            var mind = SEntMan.System<SharedMindSystem>().CreateMind(ServerSession.UserId);
            SEntMan.System<SharedMindSystem>().TransferTo(mind, recipient, mind: mind);
        });

        await RunTicks(5);

        await Server.WaitPost(() =>
        {
            var implant = SEntMan.System<SharedSubdermalImplantSystem>()
                .AddImplant(recipient, "UplinkImplant")!.Value;
            var remoteStore = SEntMan.GetComponent<RemoteStoreComponent>(implant);
            var implantComp = SEntMan.GetComponent<SubdermalImplantComponent>(implant);

            Assert.That(remoteStore.Store, Is.Not.Null);
            Assert.That(implantComp.Action, Is.Not.Null);
            implantNet = SEntMan.GetNetEntity(implant);
        });

        await RunTicks(5);

        await Client.WaitPost(() =>
        {
            var actions = Client.System<ActionsSystem>();
            var action = actions.GetActions(CPlayer).Single(action =>
                CEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == "ActionOpenUplinkImplant");
            actions.TriggerAction(action);
        });

        await RunTicks(5);

        await Server.WaitAssertion(() =>
        {
            var implant = SEntMan.GetEntity(implantNet);
            Assert.That(SUiSys.IsUiOpen((implant, null), StoreUiKey.Key, recipient), Is.True);
        });

        await Client.WaitAssertion(() =>
        {
            var implant = CEntMan.GetEntity(implantNet);
            Assert.That(CUiSys.IsUiOpen((implant, null), StoreUiKey.Key, CPlayer), Is.True);
        });
    }
    [Test]
    public async Task DynamicallyAddedIntrinsicStoreOpens()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.AddComponent(SPlayer, new IntrinsicAugmentsComponent
            {
                Action = "ActionOpenCantripStore",
                AddComponents = new ComponentRegistry
                {
                    ["Store"] = new EntityPrototype.ComponentRegistryEntry(new StoreComponent())
                }
            });
            Assert.That(SEntMan.HasComponent<StoreComponent>(SPlayer), Is.True);
        });

        await RunTicks(5);
        await Client.WaitPost(() =>
        {
            var actions = Client.System<ActionsSystem>();
            var action = actions.GetActions(CPlayer).Single(action =>
                CEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == "ActionOpenCantripStore");
            actions.TriggerAction(action);
        });

        await RunTicks(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CUiSys.IsUiOpen((CPlayer, null), StoreUiKey.Key, CPlayer), Is.True);
        });
    }

    [Test]
    public async Task PersonalAiStoreOpens()
    {
        await Server.WaitPost(() =>
        {
            var pai = SEntMan.SpawnEntity(
                "PersonalAI",
                SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            Server.PlayerMan.SetAttachedEntity(ServerSession, pai);

            var open = new IntrinsicStoreActionEvent { Performer = pai };
            SEntMan.EventBus.RaiseLocalEvent(pai, open);

            Assert.That(SUiSys.IsUiOpen((pai, null), StoreUiKey.Key, pai), Is.True);
        });
    }
}
