using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery;

public sealed partial class SurgerySystem
{
    public List<SurgeryTutorialPrototype> GetAllTutorials(Entity<SurgeryTargetComponent?> ent, ProtoId<OrganCategoryPrototype> target)
    {
        var tutorials = new List<SurgeryTutorialPrototype>();

        if (!Resolve(ent, ref ent.Comp))
            return tutorials;
        
        if (target == _defaultLimb)
        {
            foreach (var stepPack in ent.Comp.Steps)
            {
                if (!ProtoMan.TryIndex(stepPack, out var stepPackProto))
                    continue;
                
                foreach (var tutorial in stepPackProto.Tutorials)
                {
                    if (!ProtoMan.TryIndex(tutorial, out var tutorialProto))
                        continue;

                    tutorials.Add(tutorialProto);
                }
            }

            return tutorials;
        }

        if (!TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null ||
            body.Organs.Count == 0)
            return tutorials;

        var targetLimb = GetTargetLimb((ent.Owner, body), target);
        if (targetLimb == null)
            return tutorials;
        
        foreach (var stepPack in targetLimb.Value.Comp2!.Steps)
        {
            if (!ProtoMan.TryIndex(stepPack, out var stepPackProto))
                continue;
            
            foreach (var tutorial in stepPackProto.Tutorials)
            {
                if (!ProtoMan.TryIndex(tutorial, out var tutorialProto))
                    continue;
                
                tutorials.Add(tutorialProto);
            }
        }

        return tutorials;
    }
}