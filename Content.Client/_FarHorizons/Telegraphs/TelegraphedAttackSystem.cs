using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.Telegraphs;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.Telegraphs;

public sealed partial class TelegraphedAttackSystem : SharedTelegraphedAttackSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TelegraphedAttackOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new(EntityManager, _protoMan);
        _overlayMan.AddOverlay(_overlay);
        var hostileTelegraphs = _cfg.GetCVar(FHCCVars.HostileTelegraphsColor);
        var utilityTelegraphs = _cfg.GetCVar(FHCCVars.UtilityTelegraphsColor);
        _overlay.SetColors(hostileTelegraphs, utilityTelegraphs);

        _cfg.OnValueChanged(FHCCVars.HostileTelegraphsColor, c => _overlay.SetColors(c, _cfg.GetCVar(FHCCVars.UtilityTelegraphsColor)));
        _cfg.OnValueChanged(FHCCVars.UtilityTelegraphsColor, c => _overlay.SetColors(_cfg.GetCVar(FHCCVars.HostileTelegraphsColor), c));
    }
}