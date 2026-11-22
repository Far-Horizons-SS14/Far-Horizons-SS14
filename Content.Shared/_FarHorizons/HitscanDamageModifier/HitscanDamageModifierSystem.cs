using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.HitscanDamageModifier;

public sealed partial class HitscanDamageModifierSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!; 
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize() => SubscribeLocalEvent<HitscanDamageModifierComponent, GetVerbsEvent<ExamineVerb>>(OnExamineModifier);

    private void OnExamineModifier(EntityUid uid, HitscanDamageModifierComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.Damage.Empty)
            return;

        var msg = new FormattedMessage();

        msg.AddMarkupOrThrow(Loc.GetString("hitscan-damage-modifier-examine"));
        msg.AddMessage(GetDamageExamine(component.Damage));

        if (!msg.IsEmpty)
            _examine.AddDetailedExamineVerb(args, component, msg,
                Loc.GetString("hitscan-damage-modifier-verb-text"),
                "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
                Loc.GetString("hitscan-damage-modifier-verb-message")
            );
    }

    //copied from DamageExamineSystem with slight modification. rather this than uneccesarily change where this was taken from.
    private FormattedMessage GetDamageExamine(DamageSpecifier damageSpecifier)
    {
        var msg = new FormattedMessage();

        foreach (var damage in damageSpecifier.DamageDict)
            if (damage.Value != FixedPoint2.Zero)
            {
                msg.PushNewline();
                msg.AddMarkupOrThrow(Loc.GetString(damage.Value > 0
                    ? "hitscan-damage-modifier-value-positive"
                    : "hitscan-damage-modifier-value-negative"
                    , ("type", _prototype.Index(damage.Key).LocalizedName), ("amount", damage.Value)));
            }

        return msg;
    }
}