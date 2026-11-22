using System.Linq;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed partial class EntityWhitelistSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _factory = default!;

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist but for prototypes.
    /// </summary>
    public bool IsValid(EntityWhitelist list, EntProtoId Id)
    {
        if (!_proto.TryIndex(Id, out var proto))
            return false;

        list.Registrations ??= StringsToRegs(list.Components);

        if (list.Registrations != null)
        {
            foreach (var reg in list.Registrations)
            {
                if (proto.Components.ContainsKey(reg.Name))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null &&
        proto.TryComp<ItemComponent>(out var itemComp, _factory))
        {
            if (list.Sizes.Contains(itemComp.Size))
                return true;
        }

        if (list.Tags != null && proto.TryComp<TagComponent>(out var tagComp, _factory))
        {
            return list.RequireAll
                ? _tag.HasAllTags(tagComp, list.Tags)
                : _tag.HasAnyTag(tagComp, list.Tags);
        }

        return list.RequireAll;
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list but for prototypes.
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntProtoId Id)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, Id);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is not on the list but for prototypes.
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntProtoId Id)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, Id);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is on the list but for prototypes.
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntProtoId Id)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, Id);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is not on the list but for prototypes.
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntProtoId Id)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, Id);
    }

    /// <summary>
    /// Checks if any item that would pass whitelist a would also pass whitelist b.
    /// </summary>
    /// <param name="listA"></param>
    /// <param name="listB"></param>
    /// <returns>If whitelist a passes whitelist b</returns>
    /// <remarks>A null whitelist is treated as more permissive than an empty whitelist.</remarks>
    public bool WhitelistCompare(EntityWhitelist? listA, EntityWhitelist? listB)
    {
        // Treating null like a whitelist that will pass anything
        if (listB == null)
            return true;

        if (listA == null)
            return false;

        if (listB.RequireAll && !listA.RequireAll)
            return false;

        listA.Registrations ??= StringsToRegs(listA.Components);
        listB.Registrations ??= StringsToRegs(listB.Components);

        if (listA.Registrations != null && listB.Registrations != null)
        {
            foreach (var reg in listB.Registrations)
            {
                if (listA.Registrations.Contains(reg))
                {
                    if (!listB.RequireAll)
                        return true;
                }
                else if (listB.RequireAll)
                    return false;
            }
        }

        if (listA.Sizes != null && listB.Sizes != null)
        {
            foreach (var size in listB.Sizes)
            {
                if (listA.Sizes.Contains(size))
                {
                    if (!listB.RequireAll)
                        return true;
                }
                else if (listB.RequireAll)
                    return false;
            }
        }

        if (listA.Tags != null && listB.Tags != null)
        {
            return listB.RequireAll
                ? listA.Tags.All(listB.Tags.Contains)
                : listA.Tags.Any(listB.Tags.Contains);
        }

        return listB.RequireAll;
    }
}