using Content.Server.Administration.Managers;
using Content.Server.CartridgeLoader;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Store.Systems;
using Content.Shared._FarHorizons.Banking;
using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.PDA;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server._FarHorizons.Banking;

public sealed partial class BankingSystem : SharedBankingSystem
{
    [Dependency] private IPlayerRolesManager _roleMan = default!;
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private StoreSystem _store = default!;
    
    public BankAccountBalance? GetBalance(EntityUid ent)
    {
        if (!_mind.TryGetMind(ent, out var mindUid, out var mind) ||
            !_role.MindHasRole<MindBankAccessComponent>((mindUid, mind), out var role) ||
            _roleMan.GetPlayerData(ent) is not {} playerData)
            return null;
        
        var data = new BankAccountBalance(playerData.Balance, role.Value.Comp2.RoundSpendingLimit, role.Value.Comp2.Spent);
        return data;
    }

    public override bool ChangeBalance(EntityUid ent, int delta)
    {
        if (delta == 0 ||
            !_mind.TryGetMind(ent, out var mindUid, out var mind) ||
            !_role.MindHasRole<MindBankAccessComponent>((mindUid, mind), out var role) ||
            _roleMan.GetPlayerData(ent) is not {} playerData)
            return false;
        
        if (delta > 0)
        {
            playerData.Balance += delta;
            role.Value.Comp2.Spent -= delta;
            return true;
        }

        var balance = new BankAccountBalance(playerData.Balance, role.Value.Comp2.RoundSpendingLimit, role.Value.Comp2.Spent);
        var remaining = GetRemainingLimit(balance);
        
        if (Math.Abs(delta) > remaining)
            return false;
        
        playerData.Balance += delta;
        role.Value.Comp2.Spent += Math.Abs(delta);
        return true;
    }

    protected override void UpdateBankAppUi(Entity<FrontierBankAppComponent> ent, EntityUid loader, EntityUid actor)
    {
        if (!_mind.TryGetMind(actor, out var mindUid, out var mind) ||
            mind.CharacterName == null ||
            GetBalance(actor) is not {} balance)
            return;
        
        int? credstickBalance = null;

        if (TryComp<PdaComponent>(loader, out var pda) &&
            pda.CredstickSlot.ContainerSlot?.ContainedEntity is {} credstickUid &&
            TryComp<CredstickComponent>(credstickUid, out var credstick))
            credstickBalance = credstick.Balance;
        
        var state = new FrontierBankUiState((mindUid, mind), balance, credstickBalance);
        _cartridge.UpdateCartridgeUiState(loader, state);

        ent.Comp.OwnerName = mind.CharacterName;
        ent.Comp.Balance = balance;
    }
}