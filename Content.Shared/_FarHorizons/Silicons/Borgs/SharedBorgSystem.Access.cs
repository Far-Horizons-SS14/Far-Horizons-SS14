using Content.Shared.Access.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <inheritdoc/>
public abstract partial class SharedBorgSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    private void InitializeAccessModule()
    {
        SubscribeLocalEvent<BorgChassisComponent, GetAdditionalAccessEvent>(OnAdditionalAccess);
        SubscribeLocalEvent<PassiveBorgModuleComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<PassiveBorgModuleComponent, EntGotRemovedFromContainerMessage>(OnEject);
    }

    private void OnAdditionalAccess(Entity<BorgChassisComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if(!TryComp<AccessComponent>(ent.Owner, out var access) || !access.Enabled)
            return;

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
            case PassiveBorgModuleType.Armor:
                if(TryComp<ArmorBorgModuleComponent>(ent.Owner, out var abmComp))
                    _damageable.SetDamageModifierSetId(args.Container.Owner, abmComp.DamageModifierSetId);
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
            case PassiveBorgModuleType.Armor:
                _damageable.SetDamageModifierSetId(args.Container.Owner, null);
                break;
            default:
                return;
        }
    }
}