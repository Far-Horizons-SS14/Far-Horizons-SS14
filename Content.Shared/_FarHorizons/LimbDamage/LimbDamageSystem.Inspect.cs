using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{
    private static List<ProtoId<OrganCategoryPrototype>> _inspectOrder = new()
    {
        "Head",
        "Torso",
        "ArmRight",
        "HandRight",
        "ArmLeft",
        "HandLeft",
        "LegRight",
        "FootRight",
        "LegLeft",
        "FootLeft"
    };
    private static List<FixedPoint2> _inspectThresholds = new() { 0, 10, 30, 50 };
    
    private void InitInspect()
    {
        SubscribeLocalEvent<LimbDamageableComponent, GetVerbsEvent<ExamineVerb>>(OnBodyExamine);
        SubscribeLocalEvent<DamageableLimbComponent, GetVerbsEvent<ExamineVerb>>(OnDetachedExamine);
        SubscribeLocalEvent<LimbArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorExamine);
    }

    private void OnArmorExamine(Entity<LimbArmorComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var source = args.User;
        var detailsRange = _examine.IsInDetailsRange(source, ent);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateArmorMarkup(ent);
                _examine.SendExamineTooltip(source, ent, markup, false, false);
            },
            Text = Loc.GetString("limb-armor-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = Loc.GetString("limb-armor-examinable-verb-message"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/target-doll-middle.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private void OnDetachedExamine(Entity<DamageableLimbComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var source = args.User;
        var detailsRange = _examine.IsInDetailsRange(source, ent);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateDetachedMarkup(ent);
                _examine.SendExamineTooltip(source, ent, markup, false, false);
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private void OnBodyExamine(Entity<LimbDamageableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var source = args.User;
        var detailsRange = _examine.IsInDetailsRange(source, ent);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(ent);
                _examine.SendExamineTooltip(source, ent, markup, false, false);
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage CreateMarkup(Entity<LimbDamageableComponent> ent)
    {
        var msg = new FormattedMessage();

        var fullDamage = TryGetFullBodyDamage((ent, null, ent.Comp));
        var inorganicLimbs = GetInorganicOrgans(ent.AsNullable());

        if (fullDamage == null)
            return msg;

        if (fullDamage.All(p => ProcessThresholds(p.Value, _inspectThresholds).All(e => e.Value == 0)))
        {
            msg.AddMarkupOrThrow(Loc.GetString("limb-health-no-damage", ("target", Identity.Entity(ent, EntityManager))));
            return msg;
        }

        var first = true;
        foreach (var limb in _inspectOrder)
        {
            var limbName = Loc.GetString($"limb-category-limb-name-{limb.Id.ToLower()}");
            var inorganic = inorganicLimbs.Contains(limb);

            string? finalMessage = null;

            if (!fullDamage.TryGetValue(limb, out var limbDamage))
                finalMessage = Loc.GetString("limb-health-missing", ("target", Identity.Entity(ent, EntityManager)), ("limb", limbName));
            else
            {
                var thresholds = ProcessThresholds(limbDamage, _inspectThresholds);

                if (thresholds.Count != 0)
                    finalMessage = Loc.GetString("limb-health-damage-combined",
                        ("target", Identity.Entity(ent, EntityManager)), ("limb", limbName),
                        ("damage", ProcessDamageText(thresholds, inorganic)));
            }

            if (finalMessage == null) continue;
            
            if (!first)
                msg.PushNewline();
            else
                first = false;
                
            msg.AddMarkupOrThrow(finalMessage);
        }

        return msg;
    }

    public FormattedMessage CreateArmorMarkup(Entity<LimbArmorComponent> ent)
    {
        var msg = new FormattedMessage();

        if (!TryComp<ArmorComponent>(ent, out var armor))
            return msg;

        Dictionary<DamageModifierSet, List<ProtoId<OrganCategoryPrototype>>> limbArmor = new();
        limbArmor[armor.Modifiers] = new() { "Torso" };

        foreach (var (limb, proteciton) in ent.Comp.Limbs)
        {
            if (!proteciton.FlatReduction.Any() && !proteciton.Coefficients.Any())
                limbArmor[armor.Modifiers].Add(limb);
            else if (limbArmor.ContainsKey(proteciton))
                limbArmor[proteciton].Add(limb);
            else
                limbArmor[proteciton] = new() { limb };
        }

        var first = true;
        foreach (var (protection, limbs) in limbArmor)
        {
            if (!first)
            {
                msg.PushNewline();
                msg.PushNewline();
            }
            else
                first = false;

            var allLimbNames = limbs.Select(p => Loc.GetString($"limb-category-limb-name-{p.Id.ToLower()}")).ToList();
            var combinedString = string.Join(", ", allLimbNames.SkipLast(1));

            var damageMessage = allLimbNames.Count switch
            {
                0 => "",
                1 => Loc.GetString("limb-armor-examinable-header-one", ("limb", allLimbNames.First())),
                _ => Loc.GetString("limb-armor-examinable-header-many", ("limbFirst", combinedString),
                    ("limbLast", allLimbNames.Last()))
            };
            msg.AddMarkupOrThrow(damageMessage);

            foreach (var coefficientArmor in protection.Coefficients)
            {
                msg.PushNewline();

                var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                    ("type", armorType),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
                ));
            }

            foreach (var flatArmor in protection.FlatReduction)
            {
                msg.PushNewline();

                var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                    ("type", armorType),
                    ("value", flatArmor.Value)
                ));
            }
        }

        return msg;
    }

    public FormattedMessage CreateDetachedMarkup(Entity<DamageableLimbComponent> ent)
    {
        var msg = new FormattedMessage();

        if (ent.Comp.Organ?.Category == null ||
            ent.Comp.Damageable == null)
            return msg;

        var limbName = Loc.GetString($"limb-category-limb-name-{ent.Comp.Organ.Category.Value.Id.ToLower()}");
        var damage = _damageable.GetPositiveDamage((ent.Owner, ent.Comp.Damageable)).DamageDict;
        var thresholds = ProcessThresholds(damage, _inspectThresholds);
        var inorganic = _tag.HasTag(ent.Owner, InorganicTag);

        string baseMessage;

        if (thresholds.Count == 0)
            baseMessage = Loc.GetString("limb-health-examinable-detached-ok", ("limb", limbName));
        else
            baseMessage = Loc.GetString("limb-health-examinable-detached-damage-combined", ("limb", limbName),
                ("damage", ProcessDamageText(thresholds, inorganic)));
        
        msg.AddMarkupOrThrow(baseMessage);

        if (!TryComp<ConnectedOrganComponent>(ent, out var connectedOrgan) || connectedOrgan.Organs == null) return msg;

        foreach (var organ in connectedOrgan.Organs.ContainedEntities)
            if (TryComp<DamageableLimbComponent>(organ, out var damageableOrgan) &&
                damageableOrgan is { Damageable: not null, Organ: not null } &&
                damageableOrgan.Organ.Category != null)
            {
                var organName = Loc.GetString($"limb-category-limb-name-{damageableOrgan.Organ.Category.Value.Id.ToLower()}");
                var organDamage = _damageable.GetPositiveDamage((organ, damageableOrgan.Damageable)).DamageDict;
                var organThresholds = ProcessThresholds(organDamage, _inspectThresholds);
                var organInorganic = _tag.HasTag(organ, InorganicTag);

                msg.PushNewline();

                if (organThresholds.Count == 0)
                    msg.AddMarkupOrThrow(Loc.GetString("limb-health-examinable-detached-attachment-ok",
                        ("limb", organName)));
                else
                    msg.AddMarkupOrThrow(Loc.GetString("limb-health-examinable-detached-attachment-damage-combined",
                        ("limb", organName), ("damage", ProcessDamageText(organThresholds, organInorganic))));
            }

        return msg;
    }

    private Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> ProcessThresholds(Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> limbDamage, List<FixedPoint2> thresholds)
    {
        var result = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();
        foreach (var (damage, value) in limbDamage)
        {
            var matchingThresholds = thresholds.Where(p => p > 0 && p <= value).ToList();
            if (matchingThresholds.Count > 0)
                result[damage] = matchingThresholds.Last();
        }

        return result;
    }

    private string ProcessDamageText(Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> limbThresholds, bool inorganic = false)
    {
        var damagePrefix = inorganic ? "limb-health-silicon-" : "limb-health-";

        List<string> damageStrings = new();
        foreach (var (damage, threshold) in limbThresholds)
            if (Loc.TryGetString($"{damagePrefix}{damage.Id.ToLower()}-{threshold}", out var damageString))
                damageStrings.Add(damageString);

        if (damageStrings.Count == 0)
            return "";

        if (damageStrings.Count == 1)
            return damageStrings.First();

        var combinedString = string.Join(", ", damageStrings.SkipLast(1));
        return Loc.GetString("limb-health-damage-combine", ("first", combinedString), ("last", damageStrings.Last()));
    }
}