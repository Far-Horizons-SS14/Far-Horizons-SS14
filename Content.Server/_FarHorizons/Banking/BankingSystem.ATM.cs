using System.Linq;
using Content.Shared._FarHorizons.Banking.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Banking;

public sealed partial class BankingSystem
{
    [SubscribeLocalEvent]
    private void OnATMUiOpened(Entity<CredstickATMComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (GetBalance(args.Actor) is not {} balance)
            return;
        
        var state = new CredstickATMBuiState(balance);
        _ui.SetUiState(ent.Owner, CredstickATMUIKey.Key, state);
    }

    [SubscribeLocalEvent]
    private void OnATMWithdrawRequest(Entity<CredstickATMComponent> ent, ref CredstickATMRequestWithdraw args)
    {
        if (GetBalance(args.Actor) is not {} balance ||
            GetRemainingLimit(balance) < args.Amount ||
            !TryComp(ent, out TransformComponent? transform))
        {
            _audio.PlayPvs(ent.Comp.ErrorSound, ent);
            return;
        }
        
        EntProtoId? proto = null;
        int? cost = null;

        foreach (var offering in ent.Comp.CredstickOffering.OrderByDescending(p => p.Key))
        {
            if (args.Amount < offering.Key) continue;

            proto = offering.Value.Proto;
            cost = offering.Value.Cost;
            break;
        }

        if (proto == null ||
            cost == null)
        {
            _audio.PlayPvs(ent.Comp.ErrorSound, ent);
            return;
        }

        var credstick = SpawnAtPosition(proto.Value, transform.Coordinates);
        var credstickComp = EnsureComp<CredstickComponent>(credstick);

        ChangeBalance(args.Actor, -args.Amount - cost.Value);
        CredstickChangeBalance((credstick, credstickComp), args.Amount);

        _audio.PlayPvs(ent.Comp.WithdrawSound, ent);

        if (GetBalance(args.Actor) is {} newBalance)
        {
            var state = new CredstickATMBuiState(newBalance);
            _ui.SetUiState(ent.Owner, CredstickATMUIKey.Key, state);
        }
    }
}