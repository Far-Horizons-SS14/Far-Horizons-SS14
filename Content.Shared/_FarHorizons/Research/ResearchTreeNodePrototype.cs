using System.Linq;
using Content.Shared.Radio;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Research;

[Prototype]
public sealed partial class ResearchTreeNodePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;
    [DataField(required: true)]
    public ProtoId<ResearchTreeTierPrototype> Tier = default!;
    [DataField(required: true)]
    public int Cost = default!;

    [DataField]
    public List<ProtoId<ResearchTreeNodePrototype>> Requires = [];

    [DataField]
    public List<ProtoId<LatheRecipePrototype>> Unlocks = [];
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> AnnounceTo = [];

    public int GetDepth(IPrototypeManager protoMan)
    {
        List<int> parentDepths = [];
        foreach (var parent in Requires)
            parentDepths.Add(protoMan.Index(parent).GetDepth(protoMan));
        
        return parentDepths.Count == 0 ? 0 : parentDepths.OrderDescending().First() + 1;
    }

    public int GetTieredDepth(IPrototypeManager protoMan)
    {
        List<int> parentDepths = [];
        foreach (var parent in Requires)
        {
            var parentProto = protoMan.Index(parent);
            if (parentProto.Tier != Tier)
                continue;
            
            parentDepths.Add(parentProto.GetTieredDepth(protoMan));
        }

        return parentDepths.Count == 0 ? 0 : parentDepths.OrderDescending().First() + 1;
    }

    public List<ResearchTreeNodePrototype> Children(IPrototypeManager protoMan) =>
        [.. protoMan.EnumeratePrototypes<ResearchTreeNodePrototype>().Where(p => p.Requires.Contains(ID))];
}