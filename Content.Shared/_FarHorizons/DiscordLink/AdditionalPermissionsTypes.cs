namespace Content.Shared._FarHorizons.DiscordLink;

[Flags]
public enum AdditionalPermissionsTypes : ushort  // only 16 are allowed with ushort, change type when we will need more 
{ 
    Mentor = 1 << 0,
    AlphaTesterGhost = 1 << 1,
    PeacefulBypass = 1 << 2,
    RoleRequirementBypass = 1 << 3,
}
