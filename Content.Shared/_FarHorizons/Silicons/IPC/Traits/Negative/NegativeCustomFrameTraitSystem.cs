using Content.Shared._FarHorizons.IPC.Traits;
using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Silicons.IPC.Traits.Negative;

public sealed partial class MicroreactorIncompatibilityTraitSystem : IPCTraitSystem<MicroreactorIncompatibilityTraitComponent>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, MicroreactorIncompatibilityTraitComponent> ent)
    {
        if(!TryComp<IPCBatteryComponent>(ent.Owner, out var ipcBattery) || 
            !_itemSlots.TryGetSlot(ent.Owner, ipcBattery.PowerCellSlot.CellSlotId, out var cellSlot))
                return;

        _itemSlots.SetDisableEject(ent.Owner, cellSlot, true);
        var blackList = new EntityWhitelist
        {
            Tags = new List<ProtoId<TagPrototype>>()
        };
        blackList.Tags.Add("PowerCellMicroreactor");
        _itemSlots.SetBlacklist(ent.Owner, cellSlot, blackList);
    }
}

public sealed partial class HeavierFrameTraitSystem: IPCTraitSystem<HeavierFrameTraitComponent>
{
    [Dependency] private MovementSpeedModifierSystem _speedModifier = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeavierFrameTraitComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }
    protected override void TraitInit(Entity<IPCBrainHolderComponent, HeavierFrameTraitComponent> ent) 
        => _speedModifier.RefreshMovementSpeedModifiers(ent);

    private void OnRefreshMovementSpeed(Entity<HeavierFrameTraitComponent> ent, ref RefreshMovementSpeedModifiersEvent args) 
        => args.ModifySpeed(ent.Comp.SpeedModifier);
}

public sealed partial class IntegratedBatteryTraitSystem : IPCTraitSystem<IntegratedBatteryTraitComponent>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, IntegratedBatteryTraitComponent> ent)
    {
        if(!TryComp<IPCBatteryComponent>(ent.Owner, out var ipcBattery) || 
            !_itemSlots.TryGetSlot(ent.Owner, ipcBattery.PowerCellSlot.CellSlotId, out var cellSlot) ||
            !_container.TryGetContainer(ent.Owner, ipcBattery.PowerCellSlot.CellSlotId, out var container))
                return;

        Del(cellSlot.Item);
        var newBattery = SpawnNextToOrDrop("PowerCellHigh", ent.Owner);
        _container.Insert(newBattery, container, force: true);
        _itemSlots.SetDisableEject(ent.Owner, cellSlot, true);
        _itemSlots.SetSwap(ent.Owner, cellSlot, false);
    }
}