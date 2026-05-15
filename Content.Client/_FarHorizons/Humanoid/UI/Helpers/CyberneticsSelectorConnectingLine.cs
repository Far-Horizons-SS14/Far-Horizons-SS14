using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client._FarHorizons.Humanoid.UI.Helpers;

public struct CyberneticSelectorConnectingLine(
    Box2 limbBox,
    Vector2 buttonSize,
    Vector2 buttonOffset,
    bool side = false,
    Vector2? basePosition = null,
    Vector2? limbUiScale = null
)
{
    public Vector2? BasePosition = basePosition;

    private Vector2 _basePosition => BasePosition ?? Vector2.Zero;

    public Box2 LimbBox = limbBox;
    public Vector2 ButtonSize = buttonSize;
    public Vector2 ButtonOffset = buttonOffset;
    public bool Side = side;
    public Vector2? LimbUiScale = limbUiScale;
    private Vector2 _limbUiScale => LimbUiScale ?? Vector2.One;

    private readonly Color Color = Color.White;

    public UIBox2 ButtonPosition => 
        new(_basePosition + ButtonOffset, _basePosition + ButtonOffset + ButtonSize);

    public Vector2 LineStart =>
        new(ButtonPosition.Center.X + (ButtonSize.X / 2 * (Side ? -1 : 1)), ButtonPosition.Center.Y);

    public Vector2 LimbPosition =>
        new Box2(LimbBox.Left * _limbUiScale.X, LimbBox.Bottom * _limbUiScale.Y, LimbBox.Right * _limbUiScale.X,
            LimbBox.Top * _limbUiScale.Y).Center;
    
    public Vector2 LimbCenter => 
        _basePosition + LimbPosition;

    public CyberneticSelectorConnectingLine(CyberneticSelectorConnectingLine other) : this(other.LimbBox, other.ButtonSize, other.ButtonOffset, other.Side, other.BasePosition, other.LimbUiScale){}

    public CyberneticSelectorConnectingLine WithBasePosition(Vector2 position) =>
        new(this) { BasePosition = position };
    
    public CyberneticSelectorConnectingLine WithLimbUiScale(Vector2 scale) =>
        new(this) { LimbUiScale = scale };

    public void Draw(DrawingHandleScreen handle, float uiScale) => 
        handle.DrawLine(LineStart * uiScale, LimbCenter * uiScale, Color);
}