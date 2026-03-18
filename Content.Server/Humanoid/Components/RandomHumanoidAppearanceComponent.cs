using Content.Shared.Cloning; // FarHorizons
using Robust.Shared.Prototypes; // FarHorizons

namespace Content.Server.Humanoid.Components;

[RegisterComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField]
    public bool RandomizeName = true;

    //FarHorizons Start
    [DataField]
    public string namePrefix = "";

    [DataField]
    public bool lastNameOnly = false;
    
    //FarHorizons End
}

// FarHorizons Start
[RegisterComponent]
public sealed partial class RandomSpeciesComponent : Component
{
    public ProtoId<CloningSettingsPrototype> TransformCloningSettings = "RandomSpeciesSettings";
}
// FarHorizons End