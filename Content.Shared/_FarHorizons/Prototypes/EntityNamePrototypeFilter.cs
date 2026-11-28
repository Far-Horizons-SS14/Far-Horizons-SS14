using System.Globalization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared._FarHorizons.Prototypes;

/// <summary>
/// A filter over all loaded protos that title-cases entity names.
/// </summary>
public static class EntityNamePrototypeFilter
{
    public static void Apply(DataNodeDocument document)
    {
        if (document.Root is not SequenceDataNode sequence)
            return;

        foreach (var node in sequence.Sequence)
        {
            if (node is not MappingDataNode prototype
                || !prototype.TryGet<ValueDataNode>("type", out var type)
                || type.Value != "entity"
                || !prototype.TryGet<ValueDataNode>("name", out var name)
                || name.IsNull)
                continue;

            var titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Value);

            // Catch cases like "9mm" and "50th" where we have letters immediately after a digit
            // by just using the casing from the original name for those letters.
            for (var i = 1; i < name.Value.Length; i++)
            {
                if (char.IsDigit(name.Value[i - 1]) && char.IsLetter(name.Value[i]))
                {
                    titleCase = titleCase.Remove(i, 1).Insert(i, name.Value[i].ToString());
                }
            }

            if (titleCase == name.Value)
                continue;

            // There's an edge case - if we're using yaml aliases, the node here may be shared
            // with other nodes, and they may be non-name nodes. I don't think it's done anywhere,
            // but we should be safe and copy before modifying in these filters.
            var replacement = name.Copy();
            replacement.Value = titleCase;
            prototype["name"] = replacement;
        }
    }
}
