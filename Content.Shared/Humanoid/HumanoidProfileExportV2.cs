using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportV2
{
    [DataField]
    public string ForkId;

    // FH - Versioning is officially broken, goodbye backwards compatibility. SL uses version 2 for different way of storing job priorities. Wizden uses version 2 for new markings. It do be like that, treat this as a relic of times past
    [DataField]
    public int Version = 3; // Version 1: priorities in profile (wizden); Version 2: priorities on account between characters (starlight) refactors markings into organ markings (wizden); Version 3: priorities for both jobs and factions coupled together (far horizons)

    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = default!;
}
