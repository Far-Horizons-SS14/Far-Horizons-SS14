using Content.Shared._FarHorizons.CyberneticImplanter;
using Content.Shared.Body;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.CyberneticImplanter;

public sealed class CyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, CyberneticImplantDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CyberneticImplanterComponent> entity, ref CyberneticImplantDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            entity.Comp.ImplantedOrgan == null ||
            !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var OrganComp, Factory))
            return;

        _audio.PlayPredicted(entity.Comp.ImplantEndSound, entity, args.User);

        args.Handled = true;
    }
}