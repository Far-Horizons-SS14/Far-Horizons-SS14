using System.Numerics;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared._FarHorizons.StarSystem;
using Content.Shared._FarHorizons.StarSystem.Helpers;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

public partial class ShuttleNavControl
{
    private TimeSpan _lastPing = TimeSpan.Zero;
    private TimeSpan _nextPing = TimeSpan.Zero;
    private List<(Vector2, Color)> _pings = [];

    public void RescuePing(SpaceRescuePingMessage state)
    {
        _lastPing = _timing.CurTime;
        _nextPing = _lastPing + state.RefreshRate;
        _pings = state.Pings;
    }

    private void DrawRescuePings(DrawingHandleScreen handle, Matrix3x2 worldToShuttle, Matrix3x2 shuttleToView)
    {
        if (_nextPing < _timing.CurTime)
            return;
        
        var pingFreshness = Math.Clamp((float)(_timing.CurTime - _lastPing).TotalSeconds / (float)(_nextPing - _lastPing).TotalSeconds, 0f, 1f);
        var pingAnim = 1 / (1 + MathF.Exp(11 * (pingFreshness - 0.4f)));

        foreach (var (coord, color) in _pings)
        {
            var pingColor = new Color(color.R, color.G, color.B, pingAnim);
            var p = Vector2.Transform(coord, worldToShuttle * shuttleToView);
            handle.DrawCircle(p, 1 * MinimapScale, pingColor);
        }
    }

    private void DrawStarSystem(DrawingHandleScreen handle, Matrix3x2 worldToShuttle, Matrix3x2 shuttleToView, EntityUid? mapUid)
    {
        if (!_entMan.TryGetComponent<StarSystemMapComponent>(mapUid, out var starSystem) ||
            starSystem.StarSystem == null)
            return;
        
        var worldToView = worldToShuttle * shuttleToView;
        var viewScale = MathF.Sqrt((worldToView.M11 * worldToView.M11) + (worldToView.M12 * worldToView.M12));

        var starPos = Vector2.Transform(starSystem.StarSystem.Star.Position + starSystem.StarOffset, worldToView);
        var starRadius = Star.NAV_PIXEL_SIZE * starSystem.StarSystem.Star.Radius * viewScale;

        handle.DrawCircle(starPos, starRadius, starSystem.StarSystem.Star.Color.WithAlpha(0.5f));

        foreach (var planet in starSystem.StarSystem.Planets)
        {
            var planetPos = Vector2.Transform(planet.Position + starSystem.StarOffset, worldToView);
            var planetRadius = Planet.NAV_PIXEL_SIZE * planet.Radius * viewScale;
            handle.DrawCircle(planetPos, planetRadius, Color.Gray.WithAlpha(0.5f));
        }
    }
}