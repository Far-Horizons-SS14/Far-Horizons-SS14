using System.Linq;
using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryPainEffect : ISurgeryStepEffect
{
    [DataField] public DamageSpecifier Damage = new() { DamageDict = new() { {"Slash", 5f} } };
    [DataField] public ProtoId<EmotePrototype> Emote = "Scream";

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (entMan.HasComponent<PainNumbnessStatusEffectComponent>(target))
            return;
        
        entMan.System<SleepingSystem>().TryWaking(target);

        if (entMan.HasComponent<SleepingComponent>(target))
            return;
        
        // TODO: other ways to prevent pain
        
        var dmgModifier = 1f; // Faster tools deal less damage
        
        if (tool != null)
        {
            var toolComps = entMan.GetComponents(tool.Value).OfType<ISurgeryToolComponent>();

            if (toolComps.Any())
                dmgModifier = toolComps.Select(p => p.Speed).OrderDescending().First();
        }
        
        if (limb == null)
            entMan.System<DamageableSystem>().ChangeDamage(target, Damage * dmgModifier);
        else if (entMan.TryGetComponent<OrganComponent>(limb.Value, out var organ) && organ.Category != null)
            entMan.System<LimbDamageSystem>().TryChangeLimbDamage(target, organ.Category.Value, Damage * dmgModifier, out _);
        
        entMan.System<SharedChatSystem>().TryEmoteWithChat(target, Emote);
    }
}