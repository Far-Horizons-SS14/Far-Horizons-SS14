using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;

namespace Content.Server._FarHorizons.Banking;

public sealed partial class BankingSystem
{
    public void WithdrawFromCredstick(EntityUid actor, Entity<FrontierBankAppComponent> app, Entity<CartridgeLoaderComponent> pda, Entity<CredstickComponent> credstick, int amount)
    {
        if (credstick.Comp.Balance < amount ||
            !ChangeBalance(actor, amount))
            return;
        
        CredstickChangeBalance(credstick.AsNullable(), -amount);

        UpdateBankAppUi(app, pda, actor);
    }

    public void DepositToCredstick(EntityUid actor, Entity<FrontierBankAppComponent> app, Entity<CartridgeLoaderComponent> pda, Entity<CredstickComponent> credstick, int amount)
    {
        if (!ChangeBalance(actor, -amount))
            return;
        
        CredstickChangeBalance(credstick.AsNullable(), amount);

        UpdateBankAppUi(app, pda, actor);
    }

    protected override void RefreshCredstickState(Entity<CartridgeLoaderComponent> ent, Entity<CredstickComponent>? credstick)
    {
        if (!_cartridge.TryGetProgram<FrontierBankAppComponent>(ent, out var programUid, out var program, loader: ent.Comp) ||
            program.Balance == null)
            return;
        
        var newState = new FrontierBankUiState(program.OwnerName, program.Balance.Value, credstick?.Comp.Balance);
        _cartridge.UpdateCartridgeUiState(ent, newState);
    }

    [SubscribeLocalEvent]
    private void OnBankAppMessage(Entity<FrontierBankAppComponent> ent, ref CartridgeMessageEvent args)
    {
        var loader = GetEntity(args.LoaderUid);

        if (!TryComp<PdaComponent>(loader, out var pda) ||
            !TryComp<CartridgeLoaderComponent>(loader, out var cartridgeLoader) ||
            pda.CredstickSlot.ContainerSlot?.ContainedEntity is not {} credstickUid ||
            !TryComp<CredstickComponent>(credstickUid, out var credstick))
            return;

        if (args is FrontierBankCredsticDepositMessageEvent deposit)
            DepositToCredstick(args.Actor, ent, (loader, cartridgeLoader), (credstickUid, credstick), deposit.Amount);
        else if (args is FrontierBankCredsticWithdrawMessageEvent withdraw)
            WithdrawFromCredstick(args.Actor, ent, (loader, cartridgeLoader), (credstickUid, credstick), withdraw.Amount);
    }
}