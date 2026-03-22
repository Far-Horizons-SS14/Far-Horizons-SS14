using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class StaminaComponentTest
{
    [Test]
    public async Task ValidatePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protos = pair.GetPrototypesWithComponent<StaminaComponent>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (proto, comp) in protos)
                {
                    Assert.That(comp.AnimationThreshold, Is.LessThan(comp.CritThreshold),
                        $"Animation threshold on {proto.ID} must be less than its crit threshold.");
                    
                    Assert.That(comp.Decay, Is.Positive.Or.Zero, "Negative decay results in nonsensical behavior.");
                    Assert.That(comp.Cooldown, Is.Positive.Or.Zero, "Negative cooldown results in nonsensical behavior");
                    Assert.That(comp.BaseCritThreshold, Is.Positive);
                    Assert.That(comp.CritThreshold, Is.Positive);
                    Assert.That(comp.AfterCritDecayMultiplier, Is.Positive);
                    Assert.That(comp.ForceStandStamina, Is.Positive);

                    // NUnit's analyzer is defective here. Cool.
    #pragma warning disable NUnit2041
                    Assert.That(comp.StunModifierThresholds.Keys,
                        Has.All.GreaterThanOrEqualTo(FixedPoint2.Zero).And.LessThanOrEqualTo(FixedPoint2.New(1.0f)),
                        "The stun thresholds are percentages and should be in the [0, 1.0] range.");
    #pragma warning restore NUnit2041
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
