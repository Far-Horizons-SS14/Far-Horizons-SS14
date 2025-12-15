using Robust.Shared.GameStates;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using System.Numerics;

namespace Content.Shared._FarHorizons.VehicleBuckle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleBuckleComponent : Component
{
    [DataField("unbuckletime"), AutoNetworkedField]
    public TimeSpan duration = TimeSpan.FromSeconds(3f);

    [DataField("dismountonstun"), AutoNetworkedField]
    public bool stundismount = true;

    [DataField("dismountonknockdown"), AutoNetworkedField]
    public bool knockdowndismount = true;
    
    /// <summary>
    /// What the buckle offset is used for north
    /// </summary>
    [DataField("northOffset"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 NorthOffset = Vector2.Zero;

    /// <summary>
    /// What the buckle offset is used for south
    /// </summary>
    [DataField("southOffset"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 SouthOffset = Vector2.Zero;

    /// <summary>
    /// What the buckle offset is used for west
    /// </summary>
    [DataField("westOffset"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 WestOffset= Vector2.Zero;

    /// <summary>
    /// What the buckle offset is used for east
    /// </summary>
    [DataField("eastOffset"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 EastOffset = Vector2.Zero;

    /// <summary>
    /// What drawdepth is used for north
    /// </summary>
    [DataField("northDrawDepth"), AutoNetworkedField]
    public int northDrawDepth = DrawDepthTag.Default;

    /// <summary>
    /// What drawdepth is used for south
    /// </summary>
    [DataField("southDrawDepth"), AutoNetworkedField]
    public int southDrawDepth = DrawDepthTag.Default;

    /// <summary>
    /// What drawdepth is used for south
    /// </summary>
    [DataField("eastDrawDepth"), AutoNetworkedField]
    public int eastDrawDepth = DrawDepthTag.Default;

    /// <summary>
    /// What drawdepth is used for south
    /// </summary>
    [DataField("westDrawDepth"), AutoNetworkedField]
    public int westDrawDepth = DrawDepthTag.Default;
}