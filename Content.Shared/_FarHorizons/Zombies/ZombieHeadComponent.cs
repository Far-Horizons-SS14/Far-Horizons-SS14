namespace Content.Shared._FarHorizons.Zombies;

[RegisterComponent]
public sealed partial class ZombieHeadComponent : Component
{
    [DataField] public float DeathAt = 200;
}

[RegisterComponent]
public sealed partial class LegacyZombieLockComponent : Component; // Component used to "tag" mobs to use legacy zombie system (no limb damage)