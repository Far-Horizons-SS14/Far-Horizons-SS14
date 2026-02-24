using Content.Shared.Preferences;

namespace Content.Shared._FarHorizons.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles. Far Horizons version
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportFH
{
    [DataField] public string ForkId;

    [DataField] public int Version = 2; // Not used, kept for backwards compatibility

    [DataField] public int FhVersion = 1; // Only v1 exists for now

    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = default!;
}