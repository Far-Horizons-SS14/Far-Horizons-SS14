using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    private void ValidityInitialize()
    {

    }

    private bool IsValid(Entity<FusionReactorRequireValidComponent?> ent) =>
        Resolve(ent, ref ent.Comp, false) && ent.Comp.Validity == FusionReactorValidity.Valid;

    private void UpdateValidityUI(Entity<FusionReactorRequireValidComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_uiSystem.IsUiOpen(ent.Owner, FusionReactorUiKey.Key))
            return;

        _uiSystem.ServerSendUiMessage(ent.Owner, FusionReactorUiKey.Key, new FusionReactorValidityBuiMessage()
        {
            Validity = ent.Comp.Validity,
        });
    }
}