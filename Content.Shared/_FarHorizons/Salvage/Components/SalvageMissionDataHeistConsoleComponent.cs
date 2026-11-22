using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Salvage.Components;

[RegisterComponent]
public sealed partial class SalvageMissionDataHeistConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] public bool Enabled = false;
    [ViewVariables(VVAccess.ReadOnly)] public bool HasData = false;

    [DataField] public SoundSpecifier? FailSound;
    [DataField] public SoundSpecifier? SuccessSound;
    [DataField] public EntProtoId SpawnTarget = "DiskSalvageMissionObjective";
}

[RegisterComponent]
public sealed partial class SalvageMissionDataHeistPasswordBankComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] public List<int> Codes = new();
}