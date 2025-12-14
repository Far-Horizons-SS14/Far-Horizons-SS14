using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.DiscordLink;

[Prototype("discordRole")]
public sealed class DiscordRolePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    
    [DataField(required: true)]
    public ulong DiscordRoleId = default!;
    
    [DataField(required: true)]
    public string PlayerTitle = default!;
}
