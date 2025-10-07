using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExport
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 3; // Version 1: priorities in profile (wizden); Version 2: priorities on account between characters (starlight); Version 3: priorities for both jobs and factions coupled together (far horizons)

    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = default!;
}
