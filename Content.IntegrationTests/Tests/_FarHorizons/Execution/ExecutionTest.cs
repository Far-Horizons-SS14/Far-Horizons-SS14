using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;

namespace Content.IntegrationTests.Tests._FarHorizons.Execution;

public sealed class ExecutionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task PythonExecutionDealsDamage()
    {
        var damageable = SEntMan.System<DamageableSystem>();
        var targetNet = await SpawnTarget("MobHuman");
        var target = ToServer(targetNet);
        var pythonNet = await PlaceInHands("WeaponRevolverPythonAP");
        var python = ToServer(pythonNet);
        await Pair.RunSeconds(1f);

        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<StunnedComponent>(target);
            var execution = SEntMan.System<SharedExecutionSystem>();
            Assert.That(execution.CanBeExecuted(target, SPlayer), Is.True);

            var args = new DoAfterArgs(
                SEntMan,
                SPlayer,
                TickPeriod,
                new GunExecutionDoAfterEvent(),
                python,
                target,
                python);

            Assert.That(DoAfterSys.TryStartDoAfter(args));
        });

        await RunTicks(2);

        Assert.That(
            damageable.GetTotalDamage(target),
            Is.GreaterThan(FixedPoint2.Zero),
            "A completed Python execution did not damage its incapacitated target.");
    }
}
