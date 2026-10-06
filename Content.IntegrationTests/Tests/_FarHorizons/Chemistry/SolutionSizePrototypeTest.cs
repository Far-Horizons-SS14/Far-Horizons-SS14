using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._FarHorizons.Chemistry;

[TestFixture]
public sealed class SolutionSizePrototypeTest : GameTest
{
    private const int VolumePerInventorySpace = 30;
    private const string FoodSolutionId = "food";

    /// <summary>
    /// Validate that solutions, unless marked otherwise, are 30 units per inventory cell.
    /// </summary>
    [Test]
    public async Task SolutionSizesMatchItemSize()
    {
        var server = Server;
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();
        var errors = new List<string>();

        await server.WaitPost(() =>
        {
            foreach (var entity in prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (entity.Abstract ||
                    entity.TryGetComponent<TESTSolutionSizeNonStandardComponent>(out _, componentFactory) ||
                    !entity.TryGetComponent<ItemComponent>(out var item, componentFactory))
                {
                    continue;
                }

                var shape = item.Shape ?? prototypeManager.Index<ItemSizePrototype>(item.Size).DefaultShape;
                var expectedVolume = FixedPoint2.New(VolumePerInventorySpace * shape.GetArea());

                if (entity.TryGetComponent<SolutionComponent>(out var solution, componentFactory) &&
                    solution.Id != FoodSolutionId)
                    ValidateSolution(entity, entity.ID, solution, expectedVolume, errors);

                if (!entity.TryGetComponent<SolutionManagerComponent>(out var manager, componentFactory))
                    continue;

                foreach (var solutionId in manager.SolutionEnts)
                {
                    var solutionPrototype = prototypeManager.Index<EntityPrototype>(solutionId);
                    if (!solutionPrototype.TryGetComponent<SolutionComponent>(out solution, componentFactory))
                        continue;

                    if (solution.Id == FoodSolutionId)
                        continue;

                    ValidateSolution(entity, solutionId, solution, expectedVolume, errors);
                }
            }
        });

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    private static void ValidateSolution(
        EntityPrototype entity,
        EntProtoId solutionPrototypeId,
        SolutionComponent solution,
        FixedPoint2 expectedVolume,
        List<string> errors)
    {
        var actualVolume = solution.Solution.MaxVolume;
        var message = $"Entity prototype '{entity.ID}' has solution '{solutionPrototypeId}' with maxVol {actualVolume}, but its inventory footprint expects {expectedVolume}.";

        if (actualVolume > expectedVolume)
            errors.Add(message);
    }
}
