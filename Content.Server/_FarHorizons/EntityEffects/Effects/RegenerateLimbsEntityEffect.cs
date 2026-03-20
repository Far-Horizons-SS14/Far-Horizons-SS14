using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Jittering;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Polymorphs this entity into another entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class RegenerateEntityEffectSystem : EntityEffectSystem<RegrowableLimbsComponent, RegenerateLimbs>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    protected override void Effect(Entity<RegrowableLimbsComponent> ent, ref EntityEffectEvent<RegenerateLimbs> args)
    {
        if(!TryComp<BodyComponent>(ent.Owner, out var bodyComp) || bodyComp.Organs == null || !TryComp<InitialBodyComponent>(ent.Owner, out var iBodyComp))
            return;

        _jitter.DoJitter(ent.Owner, TimeSpan.FromSeconds(5), false, amplitude: 5, frequency: 10);

        Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        {
            var existingCategories = new HashSet<string>();
            var organData = new OrganProfileData(); 

            foreach (var organId in bodyComp.Organs.ContainedEntities)
            {
                if (TryComp<OrganComponent>(organId, out var organ) && organ.Category != null
                    && TryComp<VisualOrganComponent>(organId, out var vOrgan))
                {
                    existingCategories.Add(organ.Category);
                    if(organ.Category.ToString() == "Torso")
                        organData = vOrgan.Profile;
                }
            }
            foreach (var protoId in iBodyComp.Organs.Values)
            {
                if (!_proto.TryIndex(protoId, out var proto))
                    continue;

                if (!proto.TryGetComponent<OrganComponent>(out var organProto, _componentFactory))
                    continue;

                if (organProto.Category == null || existingCategories.Contains(organProto.Category))
                    continue;

                var newLimb = Spawn(protoId, Transform(ent.Owner).Coordinates);
                _visualBody.ApplyProfile(ent.Owner, organData);
                _container.Insert(newLimb, bodyComp.Organs);
            }
        });
    }
}
