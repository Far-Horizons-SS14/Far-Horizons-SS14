namespace Content.Shared._FarHorizons.VFX;

[RegisterComponent]
public sealed partial class OverlayVFXComponent : Component
{
    [DataField(required: true)] public string Shader;
}