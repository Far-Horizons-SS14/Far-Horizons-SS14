using System.Linq;
using Content.Server.NPC.HTN;
using Content.Shared._FarHorizons.Bosses;
using Content.Shared.Mobs;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Bosses;

public sealed partial class BossCombatSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

    const string HTN_TARGET_KEY = "Target";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BossCombatComponent, MapInitEvent>(OnBossInit);
        SubscribeLocalEvent<BossCombatComponent, MobStateChangedEvent>(OnBossStateChange);
    }

    private void OnBossStateChange(Entity<BossCombatComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead) return;

        foreach (var attack in ent.Comp.Attacks)
            Del(attack);
    }

    private void OnBossInit(Entity<BossCombatComponent> ent, ref MapInitEvent args)
    {
        if (!_protoMan.TryIndex<BossMechanicsSheetPrototype>(ent.Comp.Mechanics, out var mechanics))
            return;

        foreach (var mechanic in mechanics.Mechanics)
            if (mechanic.InitialCooldown != 0)
                SetCooldown(ent, mechanics.Mechanics.IndexOf(mechanic), mechanic.InitialCooldown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BossCombatComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (comp.Paused || _timing.CurTime < comp.NextUpdate) continue;
            comp.NextUpdate = _timing.CurTime + comp.RefreshRate;

            if (!htn.Blackboard.TryGetValue<EntityUid>(HTN_TARGET_KEY, out var target, EntityManager))
                continue;

            var ent = new Entity<BossCombatComponent>(uid, comp);

            CleanupCooldown(ent);

            var mechanics = GetAvailableMechanics(ent);
            var allMechanics = GetAllMechanic(ent);

            if (!allMechanics.Any()) continue;

            if (!mechanics.Any()) continue;

            var selected = _random.Pick(mechanics);
            foreach (var logic in selected.Logic)
                logic.Run(EntityManager, _random, ent);
            SetCooldown(ent, allMechanics.IndexOf(selected), selected.CooldownSeconds);
        }
    }

    public void SetPaused(Entity<BossCombatComponent?> ent, bool val)
    {
        if (!Resolve(ent, ref ent.Comp)) return;

        ent.Comp.Paused = val;
    }

    private List<BossMechanic> GetAvailableMechanics(Entity<BossCombatComponent> ent)
    {
        if (!_protoMan.TryIndex<BossMechanicsSheetPrototype>(ent.Comp.Mechanics, out var mechanics))
            return [];

        return [.. mechanics.Mechanics.Select((val, id) => (id, val))
                                      .Where(p => !ent.Comp.Cooldowns.ContainsKey(p.id))
                                      .Where(p => Consider(ent, p.val))
                                      .Select(p => p.val)
        ];
    }

    private List<BossMechanic> GetAllMechanic(Entity<BossCombatComponent> ent)
    {
        if (!_protoMan.TryIndex<BossMechanicsSheetPrototype>(ent.Comp.Mechanics, out var mechanics))
            return [];
        
        return mechanics.Mechanics;
    }

    private bool Consider(Entity<BossCombatComponent> ent, BossMechanic mechanic) =>
        !mechanic.Considerations.Any() || mechanic.Considerations.All(p => p.Consider(EntityManager, ent));

    private void CleanupCooldown(Entity<BossCombatComponent> ent) => 
        ent.Comp.Cooldowns = ent.Comp.Cooldowns.Where(p => p.Value == null || _timing.CurTime < p.Value).ToDictionary();

    private void SetCooldown(Entity<BossCombatComponent> ent, int index, int cooldown)
    {
        if (cooldown == 0)
            return;

        if (cooldown > 0)
        {
            var resolved = _timing.CurTime + TimeSpan.FromSeconds(cooldown);

            if (ent.Comp.Cooldowns.ContainsKey(index))
                ent.Comp.Cooldowns[index] = resolved;
            else
                ent.Comp.Cooldowns.Add(index, resolved);
        }
        else
        {
            if (ent.Comp.Cooldowns.ContainsKey(index))
                ent.Comp.Cooldowns[index] = null;
            else
                ent.Comp.Cooldowns.Add(index, null);
        }
    }
}