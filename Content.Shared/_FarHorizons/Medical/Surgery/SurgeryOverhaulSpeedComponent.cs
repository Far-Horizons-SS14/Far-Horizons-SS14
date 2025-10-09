namespace Content.Shared._FarHorizons.Medical.SurgeryOverhaul.SpeedModifiers.Components;

[RegisterComponent]

public sealed partial class SurgeryBedSpeedComponent : Component
{
    [DataField]
    public float BedSpeedModifier = 2.0f;
}

public sealed partial class SurgeryChemicalSpeedComponent : Component
{
    [DataField]
    public float ChemicalSpeedModifier = 1.0f;
}
