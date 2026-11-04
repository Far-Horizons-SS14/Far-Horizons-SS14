using Content.Server.GameTicking.Presets;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SecretRuleSystem
{
    private bool CheckPresetEligible(string preset) =>
        _prototypeManager.TryIndex<GamePresetPrototype>(preset, out var proto) &&
        (proto.Faction == null ||
            (_factions.GetCurrentFaction() is { } curFaction && proto.Faction == curFaction.ID)
        );
}