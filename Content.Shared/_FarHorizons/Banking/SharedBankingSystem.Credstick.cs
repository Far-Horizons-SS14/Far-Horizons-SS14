using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.Alert;
using Content.Shared.Cargo.Components;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Banking;

public abstract partial class SharedBankingSystem
{
    public const string PDA_SLOT_NAME = "id";
    private static readonly ProtoId<AlertPrototype> _transferAlert = "CredstickTransfer";

    public int CredstickGetBalance(Entity<CredstickComponent?> ent) => 
        !Resolve(ent, ref ent.Comp) ? 0 : ent.Comp.Balance;

    public void CredstickSetBalance(Entity<CredstickComponent?> ent, int balance)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        
        ent.Comp.Balance = balance;
        Dirty(ent);
        CredstickUpdateAppearance(ent, balance);
    }

    public void CredstickChangeBalance(Entity<CredstickComponent?> ent, int delta)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        
        var balance = CredstickGetBalance(ent);
        var newBalance = balance + delta;
        CredstickSetBalance(ent, newBalance);
    }

    // Searches for credstick in hand or in PDA of the entity
    // Used for transfers or other interactions where someone woud "tap" credsticks together
    public Entity<CredstickComponent>? FindCredstick(EntityUid target)
    {
        if (TryComp<HandsComponent>(target, out var hands))
        {
            Entity<HandsComponent?> handEnt = (target, hands);

            if (_hands.GetActiveItem(handEnt) is {} activeHeldItem &&
                TryComp<CredstickComponent>(activeHeldItem, out var activeHeldCredstick))
                return (activeHeldItem, activeHeldCredstick);

            var activeHand = _hands.GetActiveHand(handEnt);

            foreach (var (handId, _) in hands.Hands)
            {
                if (handId == activeHand) continue;
                
                if (_hands.GetHeldItem(handEnt, handId) is {} heldItem &&
                    TryComp<CredstickComponent>(heldItem, out var heldItemCredstick))
                    return(heldItem, heldItemCredstick);
            }
        }

        if (TryComp<InventoryComponent>(target, out var inventory) &&
            _inventory.TryGetSlotContainer(target, PDA_SLOT_NAME, out var slot, out _, inventory: inventory) &&
            slot.ContainedEntity is {} pdaEnt &&
            TryComp<PdaComponent>(pdaEnt, out var pdaComp) &&
            pdaComp.CredstickSlot.ContainerSlot?.ContainedEntity is {} credstickEnt &&
            TryComp<CredstickComponent>(credstickEnt, out var credstick))
            return (credstickEnt, credstick);
        
        return null;
    }

    public void CredstickDirectTransfer(Entity<CredstickComponent> source, Entity<CredstickDirectTransferTargetComponent> target, int amount)
    {
        if (source.Comp.Balance < amount)
            return;

        if (TryComp<CredstickComponent>(target, out var targetCredstick))
        {
            CredstickChangeBalance(source.AsNullable(), -amount);
            CredstickChangeBalance((target, targetCredstick), amount);
            return;
        }

        if (!TryComp<CargoOrderConsoleComponent>(target, out var console) ||
            _station.GetOwningStation(target) is not {} station)
            return;
        
        CredstickChangeBalance(source.AsNullable(), -amount);
        _cargo.TryAdjustBankAccount(station, console.Account, amount);
    }

    public void CredstickOpenOfferDialog(Entity<CredstickComponent> ent, EntityUid target, EntityUid user)
    {
        ent.Comp.TransferSource = user;
        ent.Comp.TransferTarget = target;
        _ui.OpenUi(ent.Owner, ent.Comp.TransferUiKey, user);
    }

    private void CredstickUpdateAppearance(Entity<CredstickComponent?> ent, int? balance = null)
    {
        balance ??= CredstickGetBalance(ent);
        _appearance.SetData(ent, CredstickContentsState.Empty, balance == 0);
    }

    private void CredstickTransferReceiverCleanup(Entity<CredstickTransferReceiverComponent> ent, bool cleanOther = true)
    {
        if (cleanOther && TryComp<CredstickTransferSenderComponent>(ent.Comp.FromEntity, out var sender))
            CredstickTransferSenderCleanup((ent.Comp.FromEntity.Value, sender), false);
        
        RemComp<CredstickTransferReceiverComponent>(ent);
    }

    private void CredstickTransferSenderCleanup(Entity<CredstickTransferSenderComponent> ent, bool cleanOther = true)
    {
        if (cleanOther && TryComp<CredstickTransferReceiverComponent>(ent.Comp.ToEntity, out var receiver))
            CredstickTransferReceiverCleanup((ent.Comp.ToEntity.Value, receiver), false);
    

        RemComp<CredstickTransferSenderComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnCredstickInit(Entity<CredstickComponent> ent, ref MapInitEvent args) =>
        CredstickUpdateAppearance(ent.AsNullable());

    [SubscribeLocalEvent]
    private void OnCanDragCredstick(Entity<CredstickComponent> ent, ref CanDragEvent args) => 
        args.Handled = true;
    
    [SubscribeLocalEvent]
    private void OnCredstickCanDrop(Entity<CredstickComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!ItemSlots.TryGetAvailableSlot(args.Target, ent, null, out _))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCredstickDragged(Entity<CredstickComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!ItemSlots.TryGetAvailableSlot(args.Target, ent, null, out var slot))
            return;
        
        ItemSlots.TryInsert(args.Target, slot, ent, null);
    }

    [SubscribeLocalEvent]
    private void OnCreditsTransfer(Entity<CredstickComponent> ent, ref CredstickTransferDialogConfirmed args)
    {
        if (ent.Comp.TransferTarget == null ||
            ent.Comp.TransferSource == null ||
            args.Amount > ent.Comp.Balance ||
            args.Amount <= 0 ||
            !_interaction.InRangeAndAccessible(ent.Comp.TransferSource.Value, ent.Comp.TransferTarget.Value))
            return;
        
        if (TryComp<CredstickDirectTransferTargetComponent>(ent.Comp.TransferTarget, out var transferTarget))
        {
            CredstickDirectTransfer(ent, (ent.Comp.TransferTarget.Value, transferTarget), args.Amount);
            return;
        }

        if (FindCredstick(ent.Comp.TransferTarget.Value) is not {} targetCredstick)
            return;
        
        var sender = EnsureComp<CredstickTransferSenderComponent>(ent.Comp.TransferSource.Value);
        var receiver = EnsureComp<CredstickTransferReceiverComponent>(ent.Comp.TransferTarget.Value);

        sender.FromEntity = receiver.FromEntity = ent.Comp.TransferSource.Value;
        sender.ToEntity = receiver.ToEntity = ent.Comp.TransferTarget.Value;

        sender.ToCredstick = receiver.ToCredstick = targetCredstick;
        sender.FromCredstick = receiver.FromCredstick = ent;

        sender.Amount = receiver.Amount = args.Amount;

        Dirty(ent.Comp.TransferSource.Value, sender);
        Dirty(ent.Comp.TransferTarget.Value, receiver);

        _popup.PopupPredicted(Loc.GetString("credstick-transfer-popup-offer"), ent.Comp.TransferSource.Value, ent.Comp.TransferSource.Value);

        ent.Comp.TransferSource = null;
        ent.Comp.TransferTarget = null;

        _alerts.ShowAlert(sender.ToEntity.Value, _transferAlert);
    }

    [SubscribeLocalEvent]
    private void OnCredstickTransferAccepted(Entity<CredstickTransferReceiverComponent> ent, ref CredstickTransferAlertEvent args)
    {
        var toCredstick = ent.Comp.ToCredstick;
        var fromCredstick = ent.Comp.FromCredstick;

        if (ent.Comp.ToCredstick == null ||
            ent.Comp.FromCredstick == null ||
            ent.Comp.FromEntity == null ||
            !TryComp<CredstickComponent>(toCredstick, out var toCredstickComp) ||
            !TryComp<CredstickComponent>(fromCredstick, out var fromCredstickComp) ||
            ent.Comp.Amount == null ||
            ent.Comp.Amount <= 0 ||
            ent.Comp.Amount > fromCredstickComp.Balance ||
            !_interaction.InRangeAndAccessible(ent.Owner, ent.Comp.FromEntity.Value))
        {
            CredstickTransferReceiverCleanup(ent);
            _alerts.ClearAlert(ent.Owner, _transferAlert);
            return;
        }

        CredstickChangeBalance((fromCredstick.Value, fromCredstickComp), -ent.Comp.Amount.Value);
        CredstickChangeBalance((toCredstick.Value, toCredstickComp), ent.Comp.Amount.Value);
        _popup.PopupEntity(Loc.GetString("credstick-transfer-popup-success"), ent);

        CredstickTransferReceiverCleanup(ent);
        _alerts.ClearAlert(ent.Owner, _transferAlert);
    }

    [SubscribeLocalEvent]
    private void OnReceiverMove(Entity<CredstickTransferReceiverComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.FromEntity == null ||
            !_interaction.InRangeAndAccessible(ent.Owner, ent.Comp.FromEntity.Value))
        {
            _popup.PopupPredicted(Loc.GetString("credstick-transfer-popup-cancelled"), ent, ent);
            CredstickTransferReceiverCleanup(ent);
            _alerts.ClearAlert(ent.Owner, _transferAlert);
        }
    }

    [SubscribeLocalEvent]
    private void OnSenderMove(Entity<CredstickTransferSenderComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.ToEntity == null ||
            !_interaction.InRangeAndAccessible(ent.Owner, ent.Comp.ToEntity.Value))
        {
            _popup.PopupPredicted(Loc.GetString("credstick-transfer-popup-cancelled"), ent, ent);
            CredstickTransferSenderCleanup(ent);
            if (ent.Comp.ToEntity != null)
                _alerts.ClearAlert(ent.Comp.ToEntity.Value, _transferAlert);
        }
    }

    [SubscribeLocalEvent]
    private void DirectTransferVerb(EntityUid uid, CredstickDirectTransferTargetComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            args.User == args.Target ||
            args.Using is null ||
            args.Using == args.Target ||
            !TryComp<CredstickComponent>(args.Using, out var credstick) ||
            credstick.Balance <= 0)
            return;
        
        args.Verbs.Add(new Verb()
        {
            Act = () => CredstickOpenOfferDialog((args.Using.Value, credstick), args.Target, args.User),
            DoContactInteraction = true,
            Text = Loc.GetString("credstick-offer-transfer"),
            IconEntity = GetNetEntity(args.Using)
        });
    }
}