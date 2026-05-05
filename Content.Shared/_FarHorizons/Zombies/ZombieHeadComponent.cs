namespace Content.Shared._FarHorizons.Zombies;

[RegisterComponent]
public sealed partial class ZombieHeadComponent : Component
{
    [DataField] public float DeathAt = 200;
}