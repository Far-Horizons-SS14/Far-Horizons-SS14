using Content.Shared.Preferences;

namespace Content.Shared._FarHorizons.Body;

[RegisterComponent]
public sealed partial class HumanoidCharacterProfileComponent : Component
{
    [ViewVariables] public HumanoidCharacterProfile? Profile;
}