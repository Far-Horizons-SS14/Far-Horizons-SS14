using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._FarHorizons.Factions;

[TestFixture]
public sealed class FactionJobAssignmentPrototypeTest : GameTest
{
    [Test]
    public async Task AllJobsHaveFactionAssignments()
    {
        var server = Server;
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var unassignedJobs = new List<string>();

        await server.WaitPost(() =>
        {
            var assignments = prototypeManager.EnumeratePrototypes<FactionJobAssignmentPrototype>().ToList();
            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (!assignments.Any(assignment => assignment.Job == job.ID))
                    unassignedJobs.Add(job.ID);
            }
        });

        Assert.That(
            unassignedJobs,
            Is.Empty,
            $"Job prototypes without a faction assignment: {string.Join(", ", unassignedJobs)}");
    }
}
