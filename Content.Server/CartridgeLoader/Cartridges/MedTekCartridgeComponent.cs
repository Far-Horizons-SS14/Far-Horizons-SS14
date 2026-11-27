using Content.Shared.MedicalScanner;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MedTekCartridgeComponent : Component
{
    [DataField] public Enum UiKeyOverride = HealthAnalyzerUiKey.Key; // Far Horizons
}
