using Content.Server.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server._FarHorizons.AutoImplanter;

public sealed partial class AutoImplanterSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ImplanterSystem _implanter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AutoImplanterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AutoImplanterComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
    }

    private void OnEntityInserted(Entity<AutoImplanterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != AutoImplanterComponent.ContainerId ||
            !TryComp<ImplanterComponent>(args.Entity, out var comp))
            return;

        _implanter.Implant(ent, ent, args.Entity, comp);
        QueueDel(args.Entity);
    }

    private void OnInit(Entity<AutoImplanterComponent> ent, ref ComponentInit args) => 
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, AutoImplanterComponent.ContainerId);
}