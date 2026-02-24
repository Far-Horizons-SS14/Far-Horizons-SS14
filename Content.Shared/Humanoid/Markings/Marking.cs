using System.Linq;
using System.Text.Json;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class Marking : IEquatable<Marking>, IComparable<Marking>, IComparable<string>
    {
        [DataField("markingColor")]
        private List<Color> _markingColors = new();

        [DataField]
        public bool IsGlowing = false; //starlight

        private Marking()
        {
        }

        public Marking(string markingId,
            List<Color> markingColors, bool isGlowing) //starlight, glowing
        {
            MarkingId = markingId;
            _markingColors = markingColors;
            IsGlowing = isGlowing; //starlight
        }

        public Marking(string markingId,
            IReadOnlyList<Color> markingColors, bool isGlowing) //starlight, glowing
            : this(markingId, new List<Color>(markingColors), isGlowing)
        {
        }

        public Marking(string markingId, int colorCount)
        {
            MarkingId = markingId;
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;
        }

        public Marking(Marking other)
        {
            MarkingId = other.MarkingId;
            _markingColors = new(other.MarkingColors);
            Forced = other.Forced;
            IsGlowing = other.IsGlowing; //starlight
        }

        /// <summary>
        ///     ID of the marking prototype.
        /// </summary>
        [DataField("markingId", required: true)]
        public string MarkingId { get; private set; } = default!;

        /// <summary>
        ///     All colors currently on this marking.
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<Color> MarkingColors => _markingColors;

        /// <summary>
        ///     If this marking should be forcefully applied, regardless of points.
        /// </summary>
        [ViewVariables]
        public bool Forced;

        public void SetColor(int colorIndex, Color color) =>
            _markingColors[colorIndex] = color;

        public void SetColor(Color color)
        {
            for (int i = 0; i < _markingColors.Count; i++)
            {
                _markingColors[i] = color;
            }
        }

        public int CompareTo(Marking? marking)
        {
            if (marking == null)
            {
                return 1;
            }

            return string.Compare(MarkingId, marking.MarkingId, StringComparison.Ordinal);
        }

        public int CompareTo(string? markingId)
        {
            if (markingId == null)
                return 1;

            return string.Compare(MarkingId, markingId, StringComparison.Ordinal);
        }

        public bool Equals(Marking? other)
        {
            if (other == null)
            {
                return false;
            }
            return MarkingId.Equals(other.MarkingId)
                && _markingColors.SequenceEqual(other._markingColors)
                && Forced.Equals(other.Forced)
                && IsGlowing.Equals(other.IsGlowing); //starlight
        }

        // VERY BIG TODO: TURN THIS INTO JSONSERIALIZER IMPLEMENTATION


        // look this could be better but I don't think serializing
        // colors is the correct thing to do
        //
        // this is still janky imo but serializing a color and feeding
        // it into the default JSON serializer (which is just *fine*)
        // doesn't seem to have compatible interfaces? this 'works'
        // for now but should eventually be improved so that this can,
        // in fact just be serialized through a convenient interface
        new public string ToString()
        {
            // reserved character
            string sanitizedName = this.MarkingId.Replace('@', '_');
            List<string> colorStringList = new();
            foreach (Color color in _markingColors)
                colorStringList.Add(color.ToHex());
            var glowing = IsGlowing ? "true" : "false";

            return $"{sanitizedName}@{String.Join(',', colorStringList)}@{glowing}";
        }

        public static Marking? ParseFromDbString(string input)
        {
            if (input.Length == 0) return null;
            var split = input.Split('@');
            if (split.Length != 3) return null;
            List<Color> colorList = new();
            foreach (string color in split[1].Split(','))
                colorList.Add(Color.FromHex(color));
            bool glowing = split[2] == "true";

            return new Marking(split[0], colorList, glowing);
        }
    }
}
