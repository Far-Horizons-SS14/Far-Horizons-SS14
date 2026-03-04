// Starlight

using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Starlight;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    [Serializable, NetSerializable]
    public struct CyberneticImplant
    {
        public string ID;
        public string Name;
        public int Cost;
        public List<string> AttachedParts;

        // Search for all entity prototypes that have 
        public static List<CyberneticImplant> GetAllCybernetics(IPrototypeManager prototypeManager) =>
            prototypeManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => p.Components.TryGetValue("RoundstartImplantable", out _))
                .Select(p =>
                {
                    if (p.Components.TryGetValue("RoundstartImplantable", out var implant) &&
                        implant.Component is RoundstartImplantableComponent implantComp)
                        return new CyberneticImplant
                        {
                            ID = p.ID,
                            Name = p.Name,
                            Cost = implantComp.Cost,
                            AttachedParts = p.Components.TryGetValue("ConnectedOrgan", out var parts) && parts.Component is ConnectedOrganComponent partComp ? partComp.Roundstart.Select(roundstart => (string)roundstart).Distinct().ToList()
                                : []
                        };
                    return new CyberneticImplant{
                        ID = "broken"
                    };
                })
                .Where(p => p.ID != "broken")
                .ToList();

        // Gets slot id for limb system
        public static string SlotIDFromBodypart(OrganComponent part) => part.Category ?? "";

    }

}
