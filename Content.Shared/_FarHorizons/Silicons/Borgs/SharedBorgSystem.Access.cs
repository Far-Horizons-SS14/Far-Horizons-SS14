using Content.Shared.Access.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <inheritdoc/>
public abstract partial class SharedBorgSystem
{
    private void InitializeAccessModule()
    {
        SubscribeLocalEvent<BorgChassisComponent, GetAdditionalAccessEvent>(OnAdditionalAccess);
        SubscribeLocalEvent<PassiveBorgModuleComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<PassiveBorgModuleComponent, EntGotRemovedFromContainerMessage>(OnEject);
    }

    private void OnAdditionalAccess(Entity<BorgChassisComponent> ent, ref GetAdditionalAccessEvent args)
    {
        foreach(var module in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if(!HasComp<PassiveBorgModuleComponent>(module) || !HasComp<AccessComponent>(module))
                continue;    

            args.Entities.Add(module);
        }
    }

    private void OnInsert(Entity<PassiveBorgModuleComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        switch(ent.Comp.PassiveType)
        {
            case PassiveBorgModuleType.Access:
                if(HasComp<BorgChassisComponent>(args.Container.Owner))
                    _access.SetAccessEnabled(ent.Owner, true);
                break;
            default:
                return;
        }
    }

    private void OnEject(Entity<PassiveBorgModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        switch(ent.Comp.PassiveType)
        {
            case PassiveBorgModuleType.Access:
                if(HasComp<BorgChassisComponent>(args.Container.Owner))
                    _access.SetAccessEnabled(ent.Owner, false);
                break;
            default:
                return;
        }
    }
}