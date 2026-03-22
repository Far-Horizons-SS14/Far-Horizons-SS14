using System.Linq;
using System.Text.Json;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Represents a marking ID and its colors
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// The <see cref="MarkingPrototype"/> referred to by this marking
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> MarkingId;

    [DataField("markingColor")]
    private List<Color> _markingColors;

    /// <summary>
    /// The colors taken on by the marking
    /// </summary>
    public IReadOnlyList<Color> MarkingColors => _markingColors;

    /// <summary>
    /// Whether the marking is forced regardless of points
    /// </summary>
    public bool Forced;

    [DataField]
    public bool IsGlowing = false; //starlight

    public Marking()
    {
        _markingColors = new();
    }

    public Marking(ProtoId<MarkingPrototype> markingId, IEnumerable<Color> colors, bool glowing = false) // Starlight
    {
        MarkingId = markingId;
        _markingColors = colors.ToList();
        IsGlowing = glowing;
    }

    public Marking(ProtoId<MarkingPrototype> markingId, int colorsCount, bool glowing = false) : this(markingId,
        Enumerable.Repeat(Color.White, colorsCount).ToList(), glowing)
    {
    }

    public bool Equals(Marking other)
    {
        return MarkingId.Equals(other.MarkingId)
            && MarkingColors.SequenceEqual(other.MarkingColors)
            && Forced.Equals(other.Forced)
            && IsGlowing.Equals(other.IsGlowing);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MarkingId, MarkingColors, Forced);
    }

    public Marking WithColor(Color color) =>
        this with { _markingColors = Enumerable.Repeat(color, MarkingColors.Count).ToList() };

    public Marking WithColorAt(int index, Color color)
    {
        var newColors = _markingColors.ShallowClone();
        newColors[index] = color;
        return this with { _markingColors = newColors };
    }

    // look this could be better but I don't think serializing
    // colors is the correct thing to do
    //
    // this is still janky imo but serializing a color and feeding
    // it into the default JSON serializer (which is just *fine*)
    // doesn't seem to have compatible interfaces? this 'works'
    // for now but should eventually be improved so that this can,
    // in fact just be serialized through a convenient interface
    public string ToLegacyDbString()
    {
        // reserved character
        string sanitizedName = MarkingId.Id.Replace('@', '_');
        List<string> colorStringList = new();
        foreach (var color in MarkingColors)
            colorStringList.Add(color.ToHex());
        var glowing = IsGlowing ? "true" : "false";

        return $"{sanitizedName}@{String.Join(',', colorStringList)}@{glowing}";
    }

    public static Marking? ParseFromDbString(string input)
    {
        if (input.Length == 0) return null;
        var split = input.Split('@');
        if (split.Length != 2) return null;
        List<Color> colorList = new();
        foreach (string color in split[1].Split(','))
        {
            colorList.Add(Color.FromHex(color));
        }
        bool glowing = split[2] == "true";

        return new Marking(split[0], colorList, glowing);
    }
}
