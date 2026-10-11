using System.Numerics;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[Prototype]
public sealed partial class LimbTargettingPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)] public LocId Name;
    [DataField] public bool HideInSettings = false;
    [DataField] public List<LimbTargetLayer> Limbs = new();
    [DataField] public List<LimbTargetLabel> Labels = new();
    [DataField] public bool DrawDebugBoxes;
}

[Serializable]
[DataDefinition]
public sealed partial class LimbTargetLayer
{
    [DataField] public ProtoId<OrganCategoryPrototype> Limb;
    [DataField] public Box2 Area;
    [DataField] public List<SpriteSpecifier> Sprites;
}

[Serializable]
[DataDefinition]
public sealed partial class LimbTargetLabel
{
    [DataField] public string Text = "";
    [DataField] public Vector2 Position;
    [DataField] public string FontPath = "/EngineFonts/NotoSans/NotoSansMono-Regular.ttf";
    [DataField] public int FontSize = 10;
}