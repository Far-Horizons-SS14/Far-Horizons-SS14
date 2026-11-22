using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components;
public sealed partial class Solution
{
    public bool FootprintEligible(IPrototypeManager? protoMan)
    {
        if (protoMan == null)
            return false;

        FixedPoint2 footprintQuantity = 0;
        FixedPoint2 noFootprintQuantity = 0;

        foreach (var (reagent, quantity) in Contents)
        {
            if (!protoMan.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                continue;
            
            if (proto.MakesFootprints)
                footprintQuantity += quantity;
            else
                noFootprintQuantity += quantity;
        }

        return footprintQuantity > noFootprintQuantity;
    }

    public bool FootprintCleanEligible(IPrototypeManager protoMan)
    {
        if (protoMan == null)
            return false;
        
        foreach (var (reagent, quantity) in Contents)
        {
            if (!protoMan.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                continue;
            
            if (proto.CleansFootprints)
                return true;
        }

        return false;
    }
}