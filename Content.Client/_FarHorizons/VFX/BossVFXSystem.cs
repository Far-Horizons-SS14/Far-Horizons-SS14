using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.VFX;

public sealed partial class BossVFXSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private BossVFXOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new(EntityManager, _protoMan);
        _overlayMan.AddOverlay(_overlay);
    }
}