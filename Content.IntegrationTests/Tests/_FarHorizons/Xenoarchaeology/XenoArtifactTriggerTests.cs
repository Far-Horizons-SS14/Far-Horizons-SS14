using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._FarHorizons.Xenoarchaeology;

[TestFixture]
public sealed class XenoArtifactTriggerTests : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: FHTestXenoArtifact
          parent: BaseXenoArtifact
          components:
          - type: XenoArtifact
            isGenerationRequired: false
        """;

    [Test]
    [TestCase("TriggerHeat", "Heat")]
    [TestCase("TriggerCold", "Cold")]
    [TestCase("TriggerRadiation", "Radiation")]
    [TestCase("TriggerBruteDamage", "Blunt")]
    public async Task DamageThresholdTriggersUnlockArtifact(string trigger, string damageType)
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifacts = entManager.System<SharedXenoArtifactSystem>();
        var damageable = entManager.System<DamageableSystem>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();

        await server.WaitPost(() =>
        {
            var artifact = SpawnArtifact(entManager);
            artifacts.CreateNode(artifact, trigger);
            var damage = prototypes.Index<DamageTypePrototype>(damageType);

            damageable.ChangeDamage(artifact.Owner, new DamageSpecifier(damage, FixedPoint2.New(20)), ignoreResistances: true);

            AssertTriggered(entManager, artifact);
        });
    }

    [Test]
    public async Task BloodReactionTriggersUnlockArtifact()
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifacts = entManager.System<SharedXenoArtifactSystem>();
        var reactive = entManager.System<ReactiveSystem>();

        await server.WaitPost(() =>
        {
            var artifact = SpawnArtifact(entManager);
            artifacts.CreateNode(artifact, "TriggerBlood");

            reactive.ReactionEntity(artifact, ReactionMethod.Touch, new ReagentQuantity("Blood", FixedPoint2.New(5)));

            AssertTriggered(entManager, artifact);
        });
    }

    [Test]
    public async Task HandInteractionTriggersUnlockArtifact()
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifacts = entManager.System<SharedXenoArtifactSystem>();

        await server.WaitPost(() =>
        {
            var artifact = SpawnArtifact(entManager);
            var user = entManager.Spawn();
            artifacts.CreateNode(artifact, "TriggerInteraction");

            entManager.EventBus.RaiseLocalEvent(artifact, new InteractHandEvent(user, artifact));

            AssertTriggered(entManager, artifact);
        });
    }

    [Test]
    public async Task DetailedExaminationTriggersUnlockArtifact()
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifacts = entManager.System<SharedXenoArtifactSystem>();

        await server.WaitPost(() =>
        {
            var artifact = SpawnArtifact(entManager);
            var user = entManager.Spawn();
            artifacts.CreateNode(artifact, "TriggerExamine");

            entManager.EventBus.RaiseLocalEvent(
                artifact,
                new ExaminedEvent(new FormattedMessage(), artifact, user, true, false));

            AssertTriggered(entManager, artifact);
        });
    }

    [Test]
    public async Task LandingAfterThrowTriggersUnlockArtifact()
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifacts = entManager.System<SharedXenoArtifactSystem>();

        await server.WaitPost(() =>
        {
            var artifact = SpawnArtifact(entManager);
            artifacts.CreateNode(artifact, "TriggerThrow");

            var landed = new LandEvent(null, false);
            entManager.EventBus.RaiseLocalEvent(artifact, ref landed);

            AssertTriggered(entManager, artifact);
        });
    }

    private static Entity<XenoArtifactComponent> SpawnArtifact(IEntityManager entManager)
    {
        var artifact = entManager.Spawn("FHTestXenoArtifact");
        return (artifact, entManager.GetComponent<XenoArtifactComponent>(artifact));
    }

    private static void AssertTriggered(IEntityManager entManager, Entity<XenoArtifactComponent> artifact)
    {
        Assert.That(entManager.HasComponent<XenoArtifactUnlockingComponent>(artifact), Is.True);
    }
}
