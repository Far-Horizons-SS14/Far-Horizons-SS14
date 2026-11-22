#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._FarHorizons.GhostRoles;

[TestFixture]
public sealed class GhostRoleEligibilityPrototypeTest : GameTest
{
    [Test]
    public async Task AllGhostRoleJobsCanBeCheckedForEligibility()
    {
        var server = Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();
        var ghostRoleSystem = entMan.System<GhostRoleSystem>();

        var ghostRoleJobs = new List<(string Source, ProtoId<JobPrototype> Job)>();
        await server.WaitPost(() =>
        {
            foreach (var prototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (prototype.TryComp<GhostRoleComponent>(out var ghostRole, componentFactory) &&
                    ghostRole is not null)
                {
                    AddGhostRoleJobs($"entity prototype '{prototype.ID}'", ghostRole);
                }
            }

            foreach (var settings in prototypeManager.EnumeratePrototypes<RandomHumanoidSettingsPrototype>())
            {
                if (settings.Components?.TryGetComponent<GhostRoleComponent>(componentFactory, out var ghostRole) == true &&
                    ghostRole is not null)
                {
                    AddGhostRoleJobs($"random humanoid settings '{settings.ID}'", ghostRole);
                }
            }
        });

        Assert.That(ghostRoleJobs, Is.Not.Empty);

        await server.WaitPost(() =>
        {
            foreach (var (source, job) in ghostRoleJobs)
            {
                Assert.DoesNotThrow(
                    () => ghostRoleSystem.GetFactionJobs([job]),
                    $"Failed to check eligibility for {source} using job '{job}'.");
            }
        });

        void AddGhostRoleJobs(string source, GhostRoleComponent ghostRole)
        {
            if (ghostRole.JobProto is { } job)
                ghostRoleJobs.Add((source, job));

            foreach (var mindRoleId in ghostRole.MindRoles)
            {
                if (!prototypeManager.TryIndex<EntityPrototype>(mindRoleId, out var mindRolePrototype) ||
                    !mindRolePrototype.TryComp<MindRoleComponent>(out var mindRole, componentFactory) ||
                    mindRole.JobPrototype is not { } mindRoleJob)
                {
                    continue;
                }

                ghostRoleJobs.Add(($"{source}, mind role '{mindRoleId}'", mindRoleJob));
            }
        }
    }
}
