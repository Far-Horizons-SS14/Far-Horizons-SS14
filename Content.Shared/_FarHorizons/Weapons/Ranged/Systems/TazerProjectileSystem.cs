using System.Linq;
using Content.Shared._FarHorizons.Weapons.Ranged.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Weapons.Ranged.Systems;

public sealed partial class TazerProjectileSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TazedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.NextUpdate)
            {
                comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateTiming);
                Taze((uid, comp));
            }
        }
    }

    #region TazerComponent
    [SubscribeLocalEvent]
    private void OnShot(Entity<TazerComponent> ent, ref AmmoShotEvent args)
    {
        if(ent.Comp.CurrentProjectiles.Count > 0)
        {
            foreach(var oldProjectiles in ent.Comp.CurrentProjectiles)
            {
                RemComp<JointVisualsComponent>(oldProjectiles);
                _projectile.EmbedDetach(oldProjectiles, null);
            }
        }

        ent.Comp.CurrentProjectiles = args.FiredProjectiles;
        foreach(var projectile in args.FiredProjectiles)
        {
            var JointVisual = EnsureComp<JointVisualsComponent>(projectile);
            JointVisual.Target = ent.Owner;
            JointVisual.Sprite = ent.Comp.tazerLine;
        }
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnFireModechanged(Entity<TazerComponent> ent, ref UseInHandEvent args)
    {
        if(ent.Comp.CurrentProjectiles.Count == 0 || args.Handled) 
            return;

        foreach(var projectile in ent.Comp.CurrentProjectiles)
            _projectile.EmbedDetach(projectile, null);
    }

    [SubscribeLocalEvent]
    private void OnContainerChange(Entity<TazerComponent> ent, ref EntGotInsertedIntoContainerMessage _)
    {
        foreach(var projectile in ent.Comp.CurrentProjectiles)
            _projectile.EmbedDetach(projectile, null);
    }
    #endregion

    #region TazerProjectileComponent
    [SubscribeLocalEvent]
    private void OnHit(Entity<TazerProjectileComponent> ent, ref ProjectileEmbedEvent args)
    {
        if (args.Weapon == null || args.Shooter == null || args.Embedded == args.Shooter.Value
            || !HasComp<BodyComponent>(args.Embedded))
            return;

        if(HasComp<TimedDespawnComponent>(ent.Owner))
            RemComp<TimedDespawnComponent>(ent.Owner);

        var tazeComp = EnsureComp<TazedComponent>(args.Embedded);
        tazeComp.User = args.Shooter.Value;
        tazeComp.Sources.Add(args.Weapon.Value);

        Dirty<TazedComponent>((args.Embedded, tazeComp));
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<TazerProjectileComponent> ent, ref InteractUsingEvent args)
    {
        var quality = "Slicing";
        if(!_tool.HasQuality(args.Used, quality))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.CuttingTime, new SimpleToolDoAfterEvent(), ent, ent, args.Used)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<TazerProjectileComponent> ent, ref SimpleToolDoAfterEvent _)
    {
        if(!TryComp<EmbeddableProjectileComponent>(ent.Owner, out var embed) 
        || !TryComp<TazedComponent>(embed.EmbeddedIntoUid, out var tazedComponent)) 
            return;

        if(tazedComponent.Sources.Count == 0)
            RemComp<TazedComponent>(embed.EmbeddedIntoUid.Value);
    
        _projectile.EmbedDetach(ent, null);
    }
    #endregion

    #region TazedComponent
    [SubscribeLocalEvent]
    private void OnStunned(Entity<TazedComponent> ent, ref StunnedEvent _)
    {
        RemComp<TazedComponent>(ent.Owner);
        foreach(var source in ent.Comp.Sources)
        {
            if(!TryComp<TazerComponent>(source, out var taser))
                return;

            var projectiles = taser.CurrentProjectiles.Where(x => TryComp<EmbeddableProjectileComponent>(x, out var embed) && embed.EmbeddedIntoUid == ent.Owner);

            foreach(var projectile in projectiles)
                _projectile.EmbedDetach(projectile, null);
        }
    }

    private void Taze(Entity<TazedComponent> ent)
    {
        foreach(var source in ent.Comp.Sources)
        {
            if(!Exists(source) || !TryComp<TazerComponent>(source, out var taser))
                continue;

            var projectiles = taser.CurrentProjectiles.Where(x => TryComp<EmbeddableProjectileComponent>(x, out var embed) && embed.EmbeddedIntoUid == ent.Owner).ToList();
            var totalProjectiles = projectiles.Count();
            foreach(var projectile in projectiles)
            {
                if(_interaction.InRangeUnobstructed(source, ent.Owner, taser.MaxDistance))
                    continue;

                _projectile.EmbedDetach(projectile, null);
                totalProjectiles=-1;
            }
                
            if(totalProjectiles == 0)
            {
                RemComp<TazedComponent>(ent.Owner);
                return;
            }

            _damage.ChangeDamage(ent.Owner, taser.DamagePerSecond, origin: source);
            _stamina.TakeStaminaDamage(ent.Owner, taser.StaminaDrainRate, source:ent.Comp.User, with:source,  
            visual:true, sound: new SoundPathSpecifier(new ResPath("/Audio/Weapons/Guns/Hits/taser_hit.ogg"), new AudioParams().WithVolume(-5)));
            _jitter.DoJitter(ent.Owner, TimeSpan.FromSeconds(1.0), true);
        }
    }
    #endregion
}