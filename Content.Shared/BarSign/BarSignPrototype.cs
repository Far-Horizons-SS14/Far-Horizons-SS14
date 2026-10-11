using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.BarSign;

[Prototype]
public sealed partial class BarSignPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;

    [DataField]
    public LocId Name { get; private set; } = "barsign-component-name";

    [DataField]
    public LocId Description { get; private set; }

    [DataField]
    public bool Hidden { get; private set; }

    // FarHorizons Start
    [DataField]
    public SignType SignType { get; private set; } = SignType.BarSign;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("color")]
    public Color? Color { get; private set; }
    // FarHorizons End
}

// FarHorizons Start
[Flags]
public enum SignType
{
    None = 0,
    BarSign = 1 << 0,
    AdSignGeneric = 1 << 1,
    AdSignNT = 1 << 2,
    AdSignNS = 1 << 3,
    AdSignGSL = 1 << 4,
}
// FarHorizons End