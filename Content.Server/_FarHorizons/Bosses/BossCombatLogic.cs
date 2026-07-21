using System.Linq;
using Content.Shared._FarHorizons.Bosses;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server._FarHorizons.Bosses;

[DataDefinition]
public sealed partial class TelegraphedAttackAroundSelf : IBossMechanicLogic
{
    [DataField(required: true)] public List<EntProtoId> Protos;

    public void Run(IEntityManager entMan, IRobustRandom random, Entity<BossCombatComponent> ent)
    {
        if (!Protos.Any()) return;

        var attack = random.Pick(Protos);

        var telegraph = entMan.System<SharedTelegraphedAttackSystem>();

        var spawned = telegraph.SpawnTelegraphAt(attack, ent);

        if (spawned == null) return;

        ent.Comp.Attacks.Add(spawned.Value);
        spawned.Value.Comp.IgnoreTargets.Add(ent);
    }
}

[DataDefinition]
public sealed partial class TelegraphedAttackFollowingEveryone : IBossMechanicLogic
{
    [DataField(required: true)] public List<EntProtoId> Protos;
    [DataField] public float SearchRange = 15; // Shouldn't be too much further than vision

    public void Run(IEntityManager entMan, IRobustRandom random, Entity<BossCombatComponent> ent)
    {
        if (!Protos.Any() ||
            ! entMan.TryGetComponent<NpcFactionMemberComponent>(ent, out var comp))
            return;

        var attack = random.Pick(Protos);

        var npcFaction = entMan.System<NpcFactionSystem>();
        var telegraph = entMan.System<SharedTelegraphedAttackSystem>();

        foreach (var enemy in npcFaction.GetNearbyHostiles((ent, comp), SearchRange))
        {
            var spawned = telegraph.SpawnTelegraphAttachedTo(attack, enemy);

            if (spawned == null) continue;

            ent.Comp.Attacks.Add(spawned.Value);
            spawned.Value.Comp.IgnoreTargets.Add(ent);
        }
    }
}

[DataDefinition]
public sealed partial class TelegraphedAttackOnFurthestTarget : IBossMechanicLogic
{
    [DataField(required: true)] public List<EntProtoId> Protos;
    [DataField] public float SearchRange = 15; // Shouldn't be too much further than vision

    public void Run(IEntityManager entMan, IRobustRandom random, Entity<BossCombatComponent> ent)
    {
        if (!Protos.Any() ||
            ! entMan.TryGetComponent<NpcFactionMemberComponent>(ent, out var comp))
            return;

        var attack = random.Pick(Protos);

        var npcFaction = entMan.System<NpcFactionSystem>();
        var transform = entMan.System<TransformSystem>();
        var telegraph = entMan.System<SharedTelegraphedAttackSystem>();

        if (!transform.TryGetMapOrGridCoordinates(ent, out var coords)) return;

        EntityUid? foundEnt = null;
        float maxDistance = 0;

        foreach (var enemy in npcFaction.GetNearbyHostiles((ent, comp), SearchRange))
        {
            if (!transform.TryGetMapOrGridCoordinates(enemy, out var tCoords) ||
                tCoords!.Value.EntityId != coords!.Value.EntityId)
                continue;
            
            var distance = (tCoords!.Value.Position - coords.Value.Position).Length();

            if (distance == 0 ||
                distance < maxDistance)
                    continue;
            
            foundEnt = enemy;
            maxDistance = distance;
        }

        if (foundEnt == null) return;

        var spawned = telegraph.SpawnTelegraphAt(attack, foundEnt.Value);

        if (spawned == null) return;

        ent.Comp.Attacks.Add(spawned.Value);
        spawned.Value.Comp.IgnoreTargets.Add(ent);
    }
}