using Robust.Shared.Random;

namespace Content.Shared._FarHorizons.Bosses;

[RegisterComponent]
public sealed partial class BossCombatComponent : Component
{
    [ViewVariables] public TimeSpan NextUpdate = TimeSpan.Zero;
    [ViewVariables] public List<EntityUid> Attacks = []; // Keeps track of all spawned attack, in case they need to be cleaned up on death
    [ViewVariables] public Dictionary<int, TimeSpan?> Cooldowns = [];
    [DataField] public bool Paused;
    [DataField(required: true)] public string Mechanics;
    [DataField] public TimeSpan RefreshRate = TimeSpan.FromSeconds(0.5);
}