using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;

namespace Content.Server._FarHorizons.Body;

public sealed partial class CustomOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomOrganComponent, OrganGotRemovedEvent>(OnCustomOrganRemoved, before: [ typeof(ConnectedOrganSystem) ]);
    }

    private void OnCustomOrganRemoved(Entity<CustomOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;

        if (HasComp<OrganComponent>(ent))
            RemComp<OrganComponent>(ent);
        
        RemComp<CustomOrganComponent>(ent);
    }
}