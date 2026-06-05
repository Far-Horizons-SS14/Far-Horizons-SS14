using Content.Shared._FarHorizons.CyberneticImplanter;
using Robust.Shared.Audio.Systems;

namespace Content.Client._FarHorizons.CyberneticImplanter;

public sealed class CyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, CyberneticImplantDoAfterEvent>(OnDoAfter);
    }
    
    private void OnDoAfter(Entity<CyberneticImplanterComponent> entity, ref CyberneticImplantDoAfterEvent args)
    {
        //we dont care if this is invalid at this point, just play the sound
        if (args.Handled || args.Cancelled)
            return;

        _audio.PlayPredicted(entity.Comp.ImplantEndSound, entity, args.User);
        
        args.Handled = true;
    }
}