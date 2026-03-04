using System.Linq;
using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;
    [DataField]
    public bool EyeGlowing { get; set; } = false; //starlight

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; set; } = new();

    [DataField]
    public float Width { get; set; } = 1f; //starlight

    [DataField]
    public float Height { get; set; } = 1f; //starlight

    public HumanoidCharacterAppearance(
        Color eyeColor,
        bool eyeGlowing, //starlight
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        float width, //starlight
        float height) //starlight
    {
        EyeColor = ClampColor(eyeColor);
        EyeGlowing = eyeGlowing; //starlight
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        Width = width; //starlight
        Height = height; //starlight
    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.EyeGlowing, other.SkinColor, new(other.Markings), other.Width, other.Height)
    {

    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight start
    public HumanoidCharacterAppearance WithEyeGlowing(bool newGlowing)
    {
        return new(EyeColor, newGlowing, SkinColor, Markings, Width, Height);
    }
    // starlight end

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, EyeGlowing, newColor, Markings, Width, Height);
    }

    //starlight start
    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return new(EyeColor, EyeGlowing, SkinColor, Markings, newWidth, Height);
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return new(EyeColor, EyeGlowing, SkinColor, Markings, Width, newHeight);
    }
    //starlight end

    public HumanoidCharacterAppearance WithMarkings(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> newMarkings)
    {
        return new(EyeColor, EyeGlowing, SkinColor, newMarkings, Width, Height);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            Color.Black,
            false, //starlight
            skinColor,
            new (),
            speciesPrototype.DefaultWidth, //starlight
            speciesPrototype.DefaultHeight //starlight
        );
        return EnsureValid(appearance, species, sex);
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        // TODO: Add random markings

        var eyeType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).EyeColoration; // Starlight

        var newEyeColor = random.Pick(_realisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;
        // Starlight - Start
        switch (eyeType)
        {
            case HumanoidEyeColor.Shadekin:
                newEyeColor = Humanoid.EyeColor.MakeShadekinValid(newEyeColor);
                break;
            default:
                break;

        }
        // Starlight - End

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        //starlight start
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var newWidth = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        var newHeight = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        //starlight end

        return new HumanoidCharacterAppearance(newEyeColor, false, newSkinColor, new (), newWidth, newHeight); //starlight, glowing
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var eyeColor = ClampColor(appearance.EyeColor);

        var width = appearance.Width; //starlight
        var height = appearance.Height; //starlight

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var skinColor = appearance.SkinColor;
        var validatedMarkings = appearance.Markings.ShallowClone();

        if (proto.TryIndex(species, out var speciesProto))
        {
            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            var organs = markingManager.GetOrgans(species);
            skinColor = strategy.EnsureVerified(skinColor);

            // Starlight - Start
            if (!Humanoid.EyeColor.VerifyEyeColor(speciesProto.EyeColoration, eyeColor))
            {
                eyeColor = Humanoid.EyeColor.ValidEyeColor(speciesProto.EyeColoration, eyeColor);
            }

            // this isn't a clamp, it's a reset if either is out of range
            // maximum is done so that small species will get the correct height if they are defaulted (1f dwarf becoming 0.8f for example)
            // minimum is done so that null values (interpreted as 0f) will get the default height and not become miniatures
            if (width > speciesProto.MaxWidth || width < speciesProto.MinWidth) width = speciesProto.DefaultWidth;
            if (height > speciesProto.MaxHeight || height < speciesProto.MinHeight) height = speciesProto.DefaultHeight;
            // Starlight - End

            foreach (var (organ, markings) in appearance.Markings)
            {
                if (!organs.ContainsKey(organ))
                    validatedMarkings.Remove(organ);
            }

            foreach (var (organ, organProtoID) in organs)
            {
                if (!markingManager.TryGetMarkingData(organProtoID, out var organData))
                {
                    validatedMarkings.Remove(organ);
                    continue;
                }

                var actualMarkings = appearance.Markings.GetValueOrDefault(organ)?.ShallowClone() ?? [];

                markingManager.EnsureValidColors(actualMarkings);
                markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
                markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
                markingManager.EnsureValidLimits(actualMarkings, organData.Value.Group, organData.Value.Layers, skinColor, eyeColor);

                validatedMarkings[organ] = actualMarkings;
            }
        }

        return new HumanoidCharacterAppearance(
            eyeColor,
            appearance.EyeGlowing, //starlight
            skinColor,
            validatedMarkings,
            width, //starlight
            height); //starlight
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EyeColor.Equals(other.EyeColor) &&
               EyeGlowing.Equals(other.EyeGlowing) && //starlight
               SkinColor.Equals(other.SkinColor) &&
               MarkingManager.MarkingsAreEqual(Markings, other.Markings) &&
               Width == other.Width && //starlight
               Height == other.Height; //starlight
    }

    // Far Horizons - to figure out what's wrong in tests
    public void Assert(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(this, other)) return;
        if (ReferenceEquals(null, other))
            throw new DebugAssertException("Appearance B is null");
        
        if (!EyeColor.Equals(other.EyeColor))
            throw new DebugAssertException($"Eye colors don't match. A: {EyeColor}; B: {other.EyeColor}");
        
        if (!EyeGlowing.Equals(other.EyeGlowing))
            throw new DebugAssertException($"Eye glow doesn't match. A: {EyeGlowing}; B: {other.EyeGlowing}");
        
        if (!SkinColor.Equals(other.SkinColor))
            throw new DebugAssertException($"Skin color doesn't match. A: {SkinColor}; B: {other.SkinColor}");
        
        if (!Width.Equals(other.Width))
            throw new DebugAssertException($"Width doesn't match. A: {Width}; B: {other.Width}");

        if (!Height.Equals(other.Height))
            throw new DebugAssertException($"Height doesn't match. A: {Height}; B: {other.Height}");

        MarkingManager.AsserMarkings(Markings, other.Markings);
    }
    // Far Horizons end

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
