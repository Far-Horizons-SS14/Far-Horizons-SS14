using System.Linq;
using System.Numerics;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Humanoid;

[Serializable, NetSerializable]
public record struct CyberneticImplant
{
    public string Id;
    public string Name;
    public int Cost;
    public List<string> Description;
    public List<string> AttachedParts;
    public ProtoId<OrganCategoryPrototype>? Slot;
    public float IconScale;
    public Vector2 IconOffset;

    // Search for all entity prototypes with RoundstartImplantable component
    public static List<CyberneticImplant> GetAllCybernetics(IPrototypeManager prototypeManager) =>
        prototypeManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract)
            .Where(p => p.Components.TryGetValue("RoundstartImplantable", out _))
            .Select(p =>
            {
                if (p.Components.TryGetValue("RoundstartImplantable", out var implant) &&
                    implant.Component is RoundstartImplantableComponent implantComp &&
                    p.Components.TryGetValue("Organ", out var organ) &&
                    organ.Component is OrganComponent organComp)
                    return new CyberneticImplant
                    {
                        Id = p.ID,
                        Name = p.Name,
                        Cost = implantComp.Cost,
                        Description = implantComp.Description.Select(d => Loc.GetString(d)).ToList(),
                        Slot = organComp.Category,
                        IconScale = implantComp.IconScale,
                        IconOffset = implantComp.IconOffset,
                        AttachedParts = p.Components.TryGetValue("ConnectedOrgan", out var parts) && parts.Component is ConnectedOrganComponent partComp ? partComp.Roundstart.Select(roundstart => (string)roundstart).Distinct().ToList()
                            : []
                    };
                return new CyberneticImplant
                {
                    Id = "broken"
                };
            })
            .Where(p => p.Id != "broken")
            .OrderBy(p => p.Cost)
            .ThenBy(p => p.Id)
            .ToList();
}