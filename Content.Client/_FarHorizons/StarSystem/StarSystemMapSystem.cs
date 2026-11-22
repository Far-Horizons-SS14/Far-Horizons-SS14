using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.StarSystem;
using Content.Shared._FarHorizons.StarSystem.Helpers;
using Content.Shared._FarHorizons.StarSystem.Prototypes;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.StarSystem;

public sealed partial class StarSystemMapSystem : SharedStarSystemMapSystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private StarOverlay _starOverlay = default!;
    private PlanetOverlay _planetOverlay = default!;
    private AsteroidBeltOverlay _beltOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _starOverlay = new(EntityManager, _protoMan);
        _planetOverlay = new(EntityManager, _protoMan);
        _beltOverlay = new(EntityManager, _protoMan);
        
        _cfg.OnValueChanged(FHCCVars.RenderStarSystem, EnsureStarSystem, true);
    }

    private void EnsureStarSystem(bool enabled)
    {
        if (enabled)
        {
            if (!_overlayMan.HasOverlay<StarOverlay>())
                _overlayMan.AddOverlay(_starOverlay);
            
            if (!_overlayMan.HasOverlay<PlanetOverlay>())
                _overlayMan.AddOverlay(_planetOverlay);
            
            if (!_overlayMan.HasOverlay<AsteroidBeltOverlay>())
                _overlayMan.AddOverlay(_beltOverlay);
        }
        else
        {
            if (_overlayMan.HasOverlay<StarOverlay>())
                _overlayMan.RemoveOverlay(_starOverlay);
            
            if (_overlayMan.HasOverlay<PlanetOverlay>())
                _overlayMan.RemoveOverlay(_planetOverlay);
            
            if (_overlayMan.HasOverlay<AsteroidBeltOverlay>())
                _overlayMan.RemoveOverlay(_beltOverlay);
            
            _starOverlay.ResetShader();
            _planetOverlay.ResetShader();
            _beltOverlay.ResetShader();
        }
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<StarSystemMapComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ent.Comp.StarSystem = MakePlanetarySystem(ent);
        _starOverlay.ResetShader();
        _planetOverlay.ResetShader();
        _beltOverlay.ResetShader();
    }

    [SubscribeLocalEvent]
    private void OnCustomStateChanged(Entity<BecomesCustomStarSystemComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Map is not {} map) return;

        var starSystem = EnsureComp<StarSystemMapComponent>(map);
        var customSystem = _protoMan.Index(ent.Comp.CustomSystem);
        starSystem.StarSystem = MakeCustomSystem(customSystem);
    }

    private PlanetarySystem MakeCustomSystem(CustomStarSystemPrototype proto)
    {
        var star = proto.Star.MakeStar();
        var planets = new List<Planet>(); // No planet support implemented yet
        var belt = (AsteroidBelt?)null; // Same for belt

        return new PlanetarySystem(star, planets, belt);
    }
}