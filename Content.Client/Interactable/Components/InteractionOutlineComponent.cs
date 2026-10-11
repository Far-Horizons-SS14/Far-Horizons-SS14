using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class InteractionOutlineComponent : Component
{
    public bool InRange;
    public int LastRenderScale;
    public bool Active;
}
