using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Research;

[Prototype]
public sealed partial class ResearchTreeStyleSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public int NodeWidth = 100;
    [DataField]
    public int NodeHeight = 30;
    [DataField]
    public int NodeSpacingHorizontal = 45;
    [DataField]
    public int NodeSpacingVertical = 35;
    [DataField]
    public int NodeMarginHorizontal = 10;
    [DataField]
    public int NodeMarginVertical = 10;
    [DataField]
    public string FontPath = "/EngineFonts/NotoSans/NotoSansMono-Regular.ttf";
    [DataField]
    public int FontSize = 16;
    [DataField]
    public string SearchIconPath = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png";
}