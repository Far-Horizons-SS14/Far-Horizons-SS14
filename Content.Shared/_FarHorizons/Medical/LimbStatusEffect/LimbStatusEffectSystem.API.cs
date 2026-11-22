using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.LimbStatusEffect;

public sealed partial class LimbStatusEffectSystem
{
    public void SetStatusEffect(Entity<LimbStatusEffectComponent?> ent, ProtoId<LimbStatusEffectPrototype> status, TimeSpan? duration = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            if (!HasComp<LimbDamageableComponent>(ent) &&
                !HasComp<DamageableLimbComponent>(ent))
                return;
            
            ent.Comp = EnsureComp<LimbStatusEffectComponent>(ent);
        }

        if (duration == null &&
            ProtoMan.Index(status).DefaultDuration is {} defaultDuration)
            ent.Comp.Statuses[status] = _timing.CurTime + defaultDuration;
        else
            ent.Comp.Statuses[status] = duration;
        
        ent.Comp.Statuses = ent.Comp.Statuses.Where(p => p.Value == null || p.Value > _timing.CurTime).ToDictionary();
        Dirty(ent);

        RefreshStatus(ent);
    }

    public void RemoveStatusEffect(Entity<LimbStatusEffectComponent?> ent, ProtoId<LimbStatusEffectPrototype> status)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;
        
        if (ent.Comp.Statuses.ContainsKey(status))
        {
            ent.Comp.Statuses.Remove(status);
            Dirty(ent);
        }
        
        RefreshStatus(ent);
    }

    public List<ProtoId<LimbStatusEffectPrototype>> GetStatusEffects(Entity<LimbStatusEffectComponent?> ent) => 
        !Resolve(ent, ref ent.Comp, false)
            ? []
            : [.. ent.Comp.Statuses.Where(p => p.Value == null || p.Value > _timing.CurTime).Select(p => p.Key)];
    
    public bool HasStatus(Entity<LimbStatusEffectComponent?> ent, ProtoId<LimbStatusEffectPrototype> status) =>
        GetStatusEffects(ent).Contains(status);
    
    public Dictionary<ProtoId<OrganCategoryPrototype>, List<ProtoId<LimbStatusEffectPrototype>>> GetStatusPerLimb(Entity<BodyComponent?> ent)
    {
        var result = new Dictionary<ProtoId<OrganCategoryPrototype>, List<ProtoId<LimbStatusEffectPrototype>>>();

        if (!Resolve(ent, ref ent.Comp))
            return result;
        
        if (TryComp<LimbStatusEffectComponent>(ent, out var bodyStatus))
        {
            var bodyStatuses = GetStatusEffects((ent, bodyStatus));
            if (bodyStatuses.Any())
                result[_defaultLimb] = bodyStatuses;
        }

        if (ent.Comp.Organs == null ||
            !ent.Comp.Organs.ContainedEntities.Any())
            return result;

        foreach (var organ in ent.Comp.Organs.ContainedEntities)
        {
            if (TryComp<LimbStatusEffectComponent>(organ, out var organStatus) &&
                TryComp<OrganComponent>(organ, out var organComp) &&
                organComp.Category != null)
            {
                var organStatuses = GetStatusEffects((organ, organStatus));
                if (organStatuses.Any())
                    result[organComp.Category.Value] = organStatuses;
            }
        }

        return result;
    }

    public List<LimbStatusEffectPrototype> GetVisible(List<LimbStatusEffectPrototype> protos)
    {
        var hidden = new HashSet<string>();
        foreach (var proto in protos)
        {
            if (!proto.Visible || proto.Hides == null)
                continue;

            foreach (var target in proto.Hides)
                hidden.Add(target.Id);
        }

        return protos.Where(p => !hidden.Contains(p.ID) && p.Visible && p.InspectText != null).ToList();
    }

    public List<LimbStatusEffectPrototype> GetDetectable(List<LimbStatusEffectPrototype> protos)
    {
        var hidden = new HashSet<string>();
        foreach (var proto in protos)
        {
            if (!proto.Detectable || proto.Hides == null)
                continue;

            foreach (var target in proto.Hides)
                hidden.Add(target.Id);
        }

        return protos.Where(p => !hidden.Contains(p.ID) && p.Detectable && p.Name != null).ToList();
    }

    public string? GetInspectText(List<ProtoId<LimbStatusEffectPrototype>> statuses)
    {
        var protos = new List<LimbStatusEffectPrototype>(statuses.Count);
        foreach (var id in statuses)
            protos.Add(ProtoMan.Index(id));

        foreach (var proto in protos)
            if (proto.HidesAll && proto.Visible)
                return proto.InspectText is { } hidingText ? Loc.GetString(hidingText) : null;

        var messages = new List<string>();
        foreach (var proto in GetVisible(protos))
            if (proto.InspectText is { } text)
                messages.Add(Loc.GetString(text));

        switch (messages.Count)
        {
            case 0:
                return null;
            case 1:
                return messages[0];
            default:
                var first = string.Join(", ", messages.Take(messages.Count - 1));
                var last = messages[^1];
                return Loc.GetString("limb-status-effect-inspect-join", ("first", first), ("last", last));
        }
    }
}