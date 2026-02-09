using System.Numerics;
using Content.Shared._FarHorizons.Shuttles;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleNavControl
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
}