using System.Linq;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;
using Content.Shared.Body;
using Content.Shared.Humanoid; // FarHorizons

namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public abstract partial class SharedSurgerySystem
{
    protected List<Type> _accents = [];
    private void InitializeConditions()
    {
        _accents = _reflectionManager.FindTypesWithAttribute<RegisterComponentAttribute>()
            .Where(type => type.Name.EndsWith("AccentComponent"))
            .ToList();

        SubscribeLocalEvent<SurgeryPartConditionComponent, SurgeryValidEvent>(OnPartConditionValid);
        SubscribeLocalEvent<SurgerySpeciesConditionComponent, SurgeryValidEvent>(OnSpeciesConditionValid);
        SubscribeLocalEvent<SurgeryOrganExistConditionComponent, SurgeryValidEvent>(OnOrganExistConditionValid);
        SubscribeLocalEvent<SurgeryOrganDontExistConditionComponent, SurgeryValidEvent>(OnOrganDontExistConditionValid);
        SubscribeLocalEvent<SurgeryAnyAccentConditionComponent, SurgeryValidEvent>(OnAnyAccentConditionValid);
    }

    // Far Horizons start
    private void OnOrganDontExistConditionValid(Entity<SurgeryOrganDontExistConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled ||
            !TryComp<BodyComponent>(args.Body, out var body) ||
            body.Organs == null) return;
        
        var filteredOrgans = body.Organs.ContainedEntities
            .Where(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == ent.Comp.Category).ToList();

        if (ent.Comp.Organ?.Count != 1)
        {
            args.Cancelled = filteredOrgans.Any();
            return;
        }

        var type = ent.Comp.Organ.Values.First().Component.GetType();

        args.Cancelled = filteredOrgans.Any(p => HasComp(p, type));
    }
    
    private void OnOrganExistConditionValid(Entity<SurgeryOrganExistConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled ||
            !TryComp<BodyComponent>(args.Body, out var body) ||
            body.Organs == null) return;

        var filteredOrgans = body.Organs.ContainedEntities
            .Where(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == ent.Comp.Category).ToList();

        if (ent.Comp.Organ?.Count != 1)
        {
            args.Cancelled = !filteredOrgans.Any();
            return;
        }
        
        var type = ent.Comp.Organ.Values.First().Component.GetType();

        args.Cancelled = !filteredOrgans.Any(p => HasComp(p, type));
    }
    // Far Horizons end

    private void OnPartConditionValid(Entity<SurgeryPartConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Parts.Count == 0)
            return;

        if (!TryComp<OrganComponent>(args.Part, out var organComp) ||
            organComp.Category == null ||
            !ent.Comp.Parts.Contains(organComp.Category.Value))
            args.Cancelled = true;
        
    }
    private void OnSpeciesConditionValid(Entity<SurgerySpeciesConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled ||
            (EntityManager.HasComponent<AnimalBypassComponent>(ent) && _tag.HasTag(args.Body, "VimPilot"))) return;
        
        if (!EntityManager.TryGetComponent<HumanoidProfileComponent>(args.Body, out var humanoidProfileComponent))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.SpeciesBlacklist.Contains(humanoidProfileComponent.Species))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.SpeciesWhitelist.Count > 0 && !ent.Comp.SpeciesWhitelist.Contains(humanoidProfileComponent.Species))
        {
            args.Cancelled = true;
            return;
        }
    }
    private void OnAnyAccentConditionValid(Entity<SurgeryAnyAccentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (args.Cancelled)
            return;
        
        foreach (var accent in _accents)
            if (HasComp(args.Body, accent))
                return;
        args.Cancelled = true;
    }
}
