using Content.Shared._FarHorizons.Telegraphs;

namespace Content.Server._FarHorizons.Telegraphs;

public sealed partial class TelegraphedAttackSystem : SharedTelegraphedAttackSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphedAttackComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<TelegraphedAttackComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.ActiveOnSpawn) return;
        Run(ent.AsNullable());
    }

    public void Run(Entity<TelegraphedAttackComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp)) return;

        ent.Comp.StartTime = Timing.CurTime;
        ent.Comp.IsActive = ent.Comp.ActiveOnSpawn;
        Dirty(ent);
    }
}