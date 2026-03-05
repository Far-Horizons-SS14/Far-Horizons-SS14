using System.Collections.Frozen;
using System.Linq;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    private FrozenDictionary<char, List<RadioChannelPrototype>> _keyCodes = default!;

    private void CacheRadios()
    {
        _keyCodes = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>()
            .GroupBy(r => r.KeyCode)
            .ToFrozenDictionary(
                group => group.Key,
                group => group.ToList());
    }

    private RadioChannelPrototype? GetBestMatchingChannel(EntityUid source,
        List<ProtoId<RadioChannelPrototype>> channels, string oldOutput, out string newOutput) =>
        GetBestMatchingChannel(source, channels.Select(c => _prototypeManager.Index(c)).ToList(), oldOutput,
            out newOutput);

    private RadioChannelPrototype? GetBestMatchingChannel(EntityUid source, List<RadioChannelPrototype> channels, string oldOutput, out string newOutput)
    {
        var availableChannels = AvailableChannels(source);

        var matchingChannels = channels.Where(channel => availableChannels.Contains(channel)).OrderBy(channel => channel.ID).ToList();

        if (matchingChannels.Count == 1)
        {
            newOutput = oldOutput;
            return matchingChannels.First();
        }
        else if (matchingChannels.Count > 1 && oldOutput.Length > 0)
        {
            var index = oldOutput[0].ToString();
            if (int.TryParse(index, out var parsedIndex))
            {
                newOutput = SanitizeMessageCapital(oldOutput[1..].TrimStart());
                return matchingChannels[parsedIndex - 1];
            }
        }

        newOutput = oldOutput;
        return null;
    }

    private List<ProtoId<RadioChannelPrototype>> AvailableChannels(EntityUid source)
    {
        List<ProtoId<RadioChannelPrototype>> result = [];

        if (TryComp<ActiveRadioComponent>(source, out var activeRadio))
            result.AddRange(activeRadio.Channels);

        if (TryComp<WearingHeadsetComponent>(source, out var wearingHeadset) &&
            TryComp<ActiveRadioComponent>(wearingHeadset.Headset, out var headsetRadio))
            result.AddRange(headsetRadio.Channels);

        return result;
    }

    public static Dictionary<ProtoId<RadioChannelPrototype>, string> GetIndexedKeycodes(
        HashSet<RadioChannelPrototype> radioChannels)
    {
        Dictionary<ProtoId<RadioChannelPrototype>, string> keyCodes = new();

        foreach (var channel in radioChannels)
        {
            var matchingCodes = radioChannels.Where(p => p.KeyCode == channel.KeyCode).OrderBy(p => p.ID).ToList();
            switch (matchingCodes.Count)
            {
                case 1:
                    keyCodes[channel.ID] = channel.KeyCode.ToString();
                    break;
                case > 1:
                    var index = matchingCodes.FindIndex(p => p.ID == channel.ID) + 1;
                    keyCodes[channel.ID] = $"{channel.KeyCode}{index}";
                    break;
            }
        }

        return keyCodes;
    }
}