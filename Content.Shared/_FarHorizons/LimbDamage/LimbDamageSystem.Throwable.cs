using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Throwing;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{

    private void InitThrowable()
    {
        SubscribeLocalEvent<LimbTargettingComponent, ThrowEvent>(OnAimedThrow);
        SubscribeLocalEvent<LimbAimedThrowComponent, LandEvent>(OnAimedLand);
    }

    private void OnAimedThrow(Entity<LimbTargettingComponent> ent, ref ThrowEvent args)
    {
        var aimedThrow = EnsureComp<LimbAimedThrowComponent>(args.Thrown);
        aimedThrow.Target = ent.Comp.Target;
    }

    private void OnAimedLand(Entity<LimbAimedThrowComponent> ent, ref LandEvent args) => 
        RemCompDeferred<LimbAimedThrowComponent>(ent);
}