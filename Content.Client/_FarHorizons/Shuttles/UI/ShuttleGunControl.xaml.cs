using System.Linq;
using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._FarHorizons.Shuttles;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._FarHorizons.Shuttles.UI;

public sealed class ShuttleGunControl : ShuttleNavControl
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputs = default!;
    private readonly SharedTransformSystem _xformSystem;
    
    private EntityUid? _shuttleEntity;
    private readonly List<(NetCoordinates coordinates, bool fill)> _gunPositions = [];
    private List<(Vector2, Color)> _tracers = [];
    
    public ShuttleGunControl() : base()
    {
        RobustXamlLoader.Load(this);
        _xformSystem = EntManager.System<SharedTransformSystem>();
    }
    
    public void SetMap(Vector2 offset, bool recentering = false)
    {
        TargetOffset = offset;
        Recentering = recentering;
    }

    public void SetShuttle(EntityUid? entity) => _shuttleEntity = entity;

    public void SetGunPositions(IEnumerable<(NetCoordinates, bool)> positions)
    {
        _gunPositions.Clear();
        _gunPositions.AddRange(positions);
    }

    public void TracerPing(BulletTracerPingMessage args) => _tracers = args.Pings;
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        /// A lot of this is a modified version of methods in <see cref="ShuttleMapControl"/>
        base.Draw(handle);
        if(_coordinates == null || _rotation == null )
            return;
        
        if (!EntManager.TryGetComponent(_coordinates.Value.EntityId, out TransformComponent? xform)
        || xform.MapID == MapId.Nullspace)
            return;

        /// The Matrix Slab, brought to you by <see cref="ShuttleNavControl"/>
        var posMatrix = Matrix3Helpers.CreateTransform(_coordinates.Value.Position, _rotation.Value);
        var ourEntRot = RotateWithEntity ? _xformSystem.GetWorldRotation(xform) : _rotation.Value;
        var ourEntMatrix = Matrix3Helpers.CreateTransform(_xformSystem.GetWorldPosition(xform), ourEntRot);
        var shuttleToWorld = Matrix3x2.Multiply(posMatrix, ourEntMatrix);
        Matrix3x2.Invert(shuttleToWorld, out var worldToShuttle);
        var shuttleToView = Matrix3x2.CreateScale(new Vector2(MinimapScale, -MinimapScale)) * Matrix3x2.CreateTranslation(MidPointVector);

        var controlLocalBounds = PixelRect;
        var realTime = _timing.RealTime;

        var mousePos = _inputs.MouseScreenPosition;
        var mouseLocalPos = GetLocalPosition(mousePos);
        Vector2? mouseNullablePos = null;

        if (mousePos.Window != WindowId.Invalid)
        {
            if (_shuttleEntity != null && controlLocalBounds.Contains(mouseLocalPos.Floored()) &&
                EntManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) &&
                shuttleXform.MapID != MapId.Nullspace)
            {
                var color = Color.Red;

                mouseNullablePos = mouseLocalPos;
                if(!_gunPositions.Any(p => p.fill))
                    handle.DrawDottedLine(MidPointVector, mouseLocalPos, color, (float) realTime.TotalSeconds * 30f);

                var mouseVerts = GetCursorObject(mouseLocalPos, Angle.Zero, scale: MinimapScale);

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, mouseVerts.Span, color.WithAlpha(0.05f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, mouseVerts.Span, color);

                Matrix3x2.Invert(worldToShuttle * shuttleToView, out var viewToWorld);
                var mapOffset = Vector2.Transform(mouseLocalPos, viewToWorld);
                var coordsText = $"{mapOffset.X:0.0}, {mapOffset.Y:0.0}";
                var coordsDimensions = handle.GetDimensions(Font, coordsText, 0.7f);
                var coordUiPosition = mouseLocalPos - new Vector2(coordsDimensions.X / 2, coordsDimensions.Y + 10);
                handle.DrawString(Font, coordUiPosition, coordsText, 0.7f, color);
            }
        }

        
        DrawGuns(handle, worldToShuttle * shuttleToView, mouseNullablePos, (float) realTime.TotalSeconds * 30f);
        DrawTracers(handle, worldToShuttle * shuttleToView);
    }

    private float GetMapObjectRadius(float scale = 1f) => WorldRange / 40f * scale;

    private ValueList<Vector2> GetCursorObject(Vector2 localPos, Angle angle, float scale = 1f, bool scalePosition = false)
    {
        // Constant size diamonds
        var diamondRadius = GetMapObjectRadius();

        var mapObj = new ValueList<Vector2>(4)
        {
            localPos + (angle.RotateVec(new Vector2(0f, -diamondRadius)) * scale),
            localPos + (angle.RotateVec(new Vector2(diamondRadius, 0f)) * scale),
            localPos + (angle.RotateVec(new Vector2(0f, diamondRadius)) * scale),
            localPos + (angle.RotateVec(new Vector2(-diamondRadius, 0f)) * scale),
        };

        if (scalePosition)
        {
            for (var i = 0; i < mapObj.Count; i++)
            {
                mapObj[i] = ScalePosition(mapObj[i]);
            }
        }

        return mapObj;
    }

    /// <summary>
    /// Eventually this can be its own shape, but for now it can just be a copy of the cursor
    /// </summary>
    private ValueList<Vector2> GetWeaponObject(Vector2 localPos, Angle angle, float scale = 1f, bool scalePosition = false) => GetCursorObject(localPos, angle, scale, scalePosition);

    /// <summary>
    /// Gets the mouse position in world coordinates, or null if the mouse is outside the window or the shuttle doesn't have a valid transform.
    /// </summary>
    /// <returns>Mouse position in world coordinates</returns>
    public Vector2? GetMousePosition()
    {
        if (!EntManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) || shuttleXform.MapID == MapId.Nullspace)
            return null;
        
        var mousePos = _inputs.MouseScreenPosition;
        var mouseLocalPos = GetLocalPosition(mousePos);

        return mousePos.Window == WindowId.Invalid
            ? null
            : InverseMapPosition(mouseLocalPos) + _xformSystem.GetMapCoordinates(shuttleXform).Position;
    }

    private void DrawGuns(DrawingHandleScreen handle, Matrix3x2 worldToView, Vector2? mousePos, float dotOffset)
    {   
        foreach (var (gunPos, fill) in _gunPositions)
        {
            var gunLocalPos = Vector2.Transform(_xformSystem.ToWorldPosition(gunPos), worldToView);
            var gunVerts = GetWeaponObject(gunLocalPos, Angle.Zero, scale: MinimapScale);

            handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, gunVerts.Span, Color.Orange);

            if(fill)
            {
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, gunVerts.Span, Color.Orange.WithAlpha(0.05f));
                if(mousePos != null)
                    handle.DrawDottedLine(gunLocalPos, mousePos.Value, Color.Orange, dotOffset);
            }
        }
    }

    private void DrawTracers(DrawingHandleScreen handle, Matrix3x2 worldToView)
    {
        foreach(var (coord, color) in _tracers)
        {
            var p = Vector2.Transform(coord, worldToView);
            handle.DrawCircle(p, 1, color);
        }
    }
}