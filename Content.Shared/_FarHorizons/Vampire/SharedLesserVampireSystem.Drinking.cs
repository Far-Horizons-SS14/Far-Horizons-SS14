using System.Linq;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Nutrition;

namespace Content.Shared._FarHorizons.Vampire;

public abstract partial class SharedLesserVampireSystem
{
    [SubscribeLocalEvent]
    private void DrinkDoAfter(Entity<LesserVampireComponent> ent, ref LesserVampireDrinkBloodDoAfterEvent args)
    {
        if (args.Target == null || args.Cancelled)
            return;

        if (TryComp<SolutionComponent>(args.Target, out var solution) &&
            TryComp<VampireDrinkableComponent>(args.Target, out var drinkable))
        {
            DoDrink(ent, (args.Target.Value, drinkable), (args.Target.Value, solution));
            args.Repeat = VampireCanDrinkBlood(ent) &&
                          CanDrinkFromContainer((args.Target.Value, solution, drinkable)) &&
                          GetBloodPool(ent) < ent.Comp.BloodPoolMax;
        }   
    }

    [SubscribeLocalEvent]
    private void BiteDoAfter(Entity<LesserVampireComponent> ent, ref LesserVampireBiteDoAfterEvent args)
    {
        if (args.Target == null || args.Cancelled)
            return;

        if (!TryComp<SolutionManagerComponent>(args.Target, out var solutionContainer) ||
            !TryComp<VampireBiteableComponent>(args.Target, out var drinkable)) return;

        var ev = new OnVampireBite(ent, (args.Target.Value, drinkable));
        RaiseLocalEvent(ent, ref ev);

        DoDrink(ent, (args.Target.Value, solutionContainer, drinkable));
        args.Repeat = VampireCanDrinkBlood(ent) &&
                      VampireCanBite(ent, (args.Target.Value, drinkable)) &&
                      CanDrinkFromBody((args.Target.Value, solutionContainer, drinkable)) &&
                      GetBloodPool(ent) < ent.Comp.BloodPoolMax;
    }

    private void DoDrink(Entity<LesserVampireComponent> ent,
        Entity<SolutionManagerComponent, VampireConsumption> target)
    {
        if (!_solution.TryGetSolution((target.Owner, target.Comp1), target.Comp2.Container, out var solution))
            return;
        
        DoDrink(ent, (target, target.Comp2), solution.Value);
    }

    private void DoDrink(Entity<LesserVampireComponent> ent,
        Entity<VampireConsumption> consumptionTarget,
        Entity<SolutionComponent> soltionTarget)
    {
        if (!TryComp<BodyComponent>(ent, out var body) || body.Organs == null)
            return;

        var stomach = body.Organs.ContainedEntities
            .Select(p =>
                TryComp<StomachComponent>(p, out var stomachComp) ? (p, stomachComp) : (Entity<StomachComponent>?)null)
            .FirstOrDefault(p => p != null);

        if (stomach == null)
        {
            if (!TryComp<StomachComponent>(ent, out var bodyStomach))
                return;
            
            stomach = (ent, bodyStomach);
        }

        var split = _solution.SplitSolution(soltionTarget, consumptionTarget.Comp.Amount);
        var ingestEv = new IngestingEvent(soltionTarget, split, false);
        RaiseLocalEvent(ent, ref ingestEv);

        _reaction.DoEntityReaction(ent, split, ReactionMethod.Ingestion);

        if (!_stomach.TryTransferSolution((stomach.Value, stomach.Value.Comp), split))
        {
            _solution.AddSolution(soltionTarget, split);
            return;
        }

        _audio.PlayPredicted(ent.Comp.DrinkSound, ent, ent);
    }

    public bool CanDrinkFromBody(Entity<SolutionManagerComponent, VampireConsumption> target) =>
        _solution.TryGetSolution((target.Owner, target.Comp1), target.Comp2.Container, out var solution) && solution.Value.Comp.Solution.Volume >= target.Comp2.Amount;

    public bool CanDrinkFromContainer(Entity<SolutionComponent, VampireConsumption> target) => 
        target.Comp1.Solution.Volume >= target.Comp2.Amount;

    public bool VampireCanDrinkBlood(Entity<LesserVampireComponent> ent)
    {
        if (!_ingestion.HasMouthAvailable(ent.Owner, ent.Owner)) return false;

        var ev = new VampireDrinkCheck(ent);
        RaiseLocalEvent(ent, ref ev);

        return !ev.Cancelled;
    }

    public bool VampireCanBite(Entity<LesserVampireComponent> ent, Entity<VampireBiteableComponent?> target)
    {
        if (!Resolve(target, ref target.Comp)) return false;

        var ev = new VampireBiteCheck(ent, target!);
        RaiseLocalEvent(ent, ref ev);

        return !ev.Cancelled;
    }
}
