using Content.Shared._FarHorizons.IPC.Traits;
using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Silicons.IPC.Traits.Positive;

public sealed partial class CyborgModuleTraitSystem : IPCTraitSystem<CyborgModuleTraitComponent>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CyborgModuleTraitComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<CyborgModuleTraitComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
    }
    protected override void TraitInit(Entity<IPCBrainHolderComponent, CyborgModuleTraitComponent> ent)
    {
        var borgSlot = new ItemSlot()
        {
            Name = "Borg Module",
            Whitelist = new EntityWhitelist()
            {
                Tags = new List<ProtoId<TagPrototype>>()
                {
                    "BorgModuleIPCCompatible"
                }
            },
        };

        _itemSlots.AddItemSlot(ent.Owner, ent.Comp2.ModuleSlotId, borgSlot);

        _itemSlots.SetBlacklist(ent.Owner, borgSlot, new EntityWhitelist()
        {
            Tags = new List<ProtoId<TagPrototype>>()
            {
                "BorgModuleIPCIncompatible"
            }
        }, replaceExisting: true);
        var module = SpawnNextToOrDrop("CyborgModuleSelector", ent.Owner);
        _hands.TryPickupAnyHand(ent.Owner, module, animate: false, animateUser: false);
    }
    private void OnItemSlotEjectAttempt(Entity<CyborgModuleTraitComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_itemSlots.TryGetSlot(ent, ent.Comp.ModuleSlotId, out var moduleSlot) ||
            moduleSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }

    private void OnItemSlotInsertAttempt(Entity<CyborgModuleTraitComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_itemSlots.TryGetSlot(ent, ent.Comp.ModuleSlotId, out var moduleSlot) ||
            moduleSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }
}

public sealed partial class OverclockingTraitSystem : IPCToggleActionTraitSystem<OverclockingTraitComponent, OverclockingTraitEvent>
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private PowerCellSystem _power = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OverclockingTraitComponent, BeforeDoAfterEvent>(BeforeDoAfter);
    }

    private void BeforeDoAfter(Entity<OverclockingTraitComponent> ent, ref BeforeDoAfterEvent args)
    {
        if(!ent.Comp.Toggled) return;

        args.Args.Delay = ent.Comp.speedModifier * args.Args.Delay;
    }

    protected override void OnToggled(Entity<IPCBrainHolderComponent, OverclockingTraitComponent> ent, bool toggle)
    {
        if(!TryComp<PowerCellDrawComponent>(ent.Owner, out var pcdComp))
            return;

        if(toggle)
        {
            _power.SetDrawRate( ent.Owner, pcdComp.DrawRate * ent.Comp2.drawRateMultiplier);
            _status.TrySetStatusEffectDuration(ent.Owner, "StatusEffectIPCFanDisabled");
        }
        else if(!toggle)
        {
            _power.SetDrawRate( ent.Owner, pcdComp.DrawRate / ent.Comp2.drawRateMultiplier);
            _status.TryRemoveStatusEffect(ent.Owner, "StatusEffectIPCFanDisabled");
        }
    }

    protected override void OnDeath(Entity<OverclockingTraitComponent> ent, ref MobStateChangedEvent args) => base.OnDeath(ent, ref args);
}

public sealed partial class RepairNanitesTraitSystem : IPCToggleActionTraitSystem<RepairNanitesTraitComponent, RepairNanitesTraitEvent>
{
    [Dependency] private PowerCellSystem _power = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, RepairNanitesTraitComponent> ent)
    {
        base.TraitInit(ent);

        if(!TryComp<PassiveDamageComponent>(ent.Owner, out var psdComp)) return;

        ent.Comp2.oldDamage = psdComp.Damage;
        ent.Comp2.oldDamageCap = psdComp.DamageCap;
        ent.Comp2.oldAllowedStates = psdComp.AllowedStates;
    }

    protected override void OnToggled(Entity<IPCBrainHolderComponent, RepairNanitesTraitComponent> ent, bool toggle)
    {
        if(!TryComp<PowerCellDrawComponent>(ent.Owner, out var pcdComp) || !TryComp<PassiveDamageComponent>(ent.Owner, out var psdComp))
            return;
            
        if(toggle)
        {
            _power.SetDrawRate( ent.Owner, pcdComp.DrawRate * ent.Comp2.drawRateMultiplier);
            _status.TrySetStatusEffectDuration(ent.Owner, "StatusEffectIPCFanDisabled");
            psdComp.Damage = ent.Comp2.Damage;
            psdComp.DamageCap = ent.Comp2.DamageCap;
            psdComp.AllowedStates = ent.Comp2.AllowedStates;
            Dirty(ent.Owner, psdComp);
        }
        else if(!toggle)
        {
            _power.SetDrawRate( ent.Owner, pcdComp.DrawRate / ent.Comp2.drawRateMultiplier);
            _status.TryRemoveStatusEffect(ent.Owner, "StatusEffectIPCFanDisabled");
            psdComp.Damage = ent.Comp2.oldDamage;
            psdComp.DamageCap = ent.Comp2.oldDamageCap;
            psdComp.AllowedStates = ent.Comp2.oldAllowedStates;
            Dirty(ent.Owner, psdComp);
        }
    }

    protected override void OnDeath(Entity<RepairNanitesTraitComponent> ent, ref MobStateChangedEvent args) => base.OnDeath(ent, ref args);
}

public sealed partial class BloodPoweredTraitSystem : IPCTraitSystem<BloodPoweredTraitComponent>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, BloodPoweredTraitComponent> ent)
    {
        if(!TryComp<IPCBatteryComponent>(ent.Owner, out var ipcBattery))
            return;

        ipcBattery.DrainAllowedTargets.Clear();
        Dirty(ent.Owner, ipcBattery);
        
        if(!_itemSlots.TryGetSlot(ent.Owner, ipcBattery.PowerCellSlot.CellSlotId, out var cellSlot))
            return;

        _itemSlots.SetDisableEject(ent.Owner, cellSlot, true);
        _itemSlots.SetSwap(ent.Owner, cellSlot, false);
    }
}

public sealed partial class LanguageDatabaseTraitSystem : IPCTraitSystem<LanguageDatabaseTraitComponent>
{
    [Dependency] private SharedImplanterSystem _implanter = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, LanguageDatabaseTraitComponent> ent)
    {
        var implant = SpawnNextToOrDrop("LanguageDatabaseImplanter", ent.Owner);
        _implanter.Implant(ent.Owner, ent.Owner, implant, Comp<ImplanterComponent>(implant));
        QueueDel(implant);
    }
}