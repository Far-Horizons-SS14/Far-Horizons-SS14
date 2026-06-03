using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.GenericPaintable;

/// <summary>
/// generic component for paintable entities, should always be used in combination with PaintableComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GenericPaintableComponent : Component
{
}