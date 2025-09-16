using Content.Shared.FarHorizons.Tools.HandheldRadio.Components;
using Robust.Client.GameObjects;

namespace Content.Client.FarHorizons.Tools.HandheldRadio;


public enum RadioLayers {
    Frame,
    Screen,
    FlapOpen,
    FlapClosed
}

public sealed class HandheldRadioSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldRadioComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<HandheldRadioComponent> uid, ref AfterAutoHandleStateEvent _){
        if (!TryComp(uid, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((uid, sprite), RadioLayers.Screen, out int id_screen, true) ||
            !_sprite.LayerMapTryGet((uid, sprite), RadioLayers.FlapOpen, out int id_flap_open, true) ||
            !_sprite.LayerMapTryGet((uid, sprite), RadioLayers.FlapClosed, out int id_flap_closed, true))
            return;

        _sprite.LayerSetVisible((uid, sprite), id_screen, uid.Comp.SpeakerEnabled);
        _sprite.LayerSetVisible((uid, sprite), id_flap_open, uid.Comp.MicEnabled);
        _sprite.LayerSetVisible((uid, sprite), id_flap_closed, !uid.Comp.MicEnabled);
    }
}
