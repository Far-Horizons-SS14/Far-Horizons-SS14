using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.Body;

public abstract partial class SharedConnectedOrganSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConnectedOrganComponent, ComponentInit>(OnConnectedOrganInit);
    }

    protected virtual void OnConnectedOrganInit(Entity<ConnectedOrganComponent> ent, ref ComponentInit args) => 
        ent.Comp.Organs = _container.EnsureContainer<Container>(ent, ConnectedOrganComponent.ContainerID);
}