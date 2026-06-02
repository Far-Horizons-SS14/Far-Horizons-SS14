using Content.Shared.Actions.Events;

namespace Content.Shared._FarHorizons.Mobs;

public abstract partial class SharedActiveCritSystem
{
    private void InitializeActions()
    {
        SubscribeLocalEvent<BlockActionInCritComponent, ActionValidateEvent>(OnActionCheck);
    }

    private void OnActionCheck(Entity<BlockActionInCritComponent> ent, ref ActionValidateEvent args)
    {
        if (_mobState.IsCritical(args.User))
            args.Invalid = true;
    }
}