using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

public sealed class CyberneticIMplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CyberneticImplanterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    //using on self
    private void OnUse(Entity<CyberneticImplanterComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryImplant(entity, args.User, args.User))
            args.Handled = true;
    }


    //using on somebody else (or self if thats who they interatcted with)
    private void OnAfterInteract(Entity<CyberneticImplanterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryImplant(entity, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryImplant(Entity<CyberneticImplanterComponent> entity, EntityUid user1, EntityUid user2)
    {
        _audio.PlayPredicted(entity.Comp.ImplantBeginSound, entity, user2);
        return true;
    }
}