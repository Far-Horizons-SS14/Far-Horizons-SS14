namespace Content.Shared._FarHorizons.Telegraphs.Effects;

[RegisterComponent]
public sealed partial class TelegraphDeleteEntityOnTriggerComponent : Component
{
    public EntityUid? Entity;
}