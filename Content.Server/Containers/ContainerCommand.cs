using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Containers;
using Robust.Shared.Toolshed;

namespace Content.Server.Containers;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class ContainerCommand : ToolshedCommand
{
    private SharedContainerSystem? _container;

    [CommandImplementation("contents")]
    public IEnumerable<EntityUid> ContainerQuery([PipedArgument] IEnumerable<EntityUid> storageEnts, string id) =>
        storageEnts.SelectMany(x => ContainerQueryBase(x, id));


    public IEnumerable<EntityUid> ContainerQueryBase(EntityUid ent, string id)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(ent, id, out var container))
            return [];

        return container.ContainedEntities;
    }

    [CommandImplementation("get")]
    public IEnumerable<BaseContainer> ContainerGet([PipedArgument] IEnumerable<EntityUid> storageEnts, string id) =>
        storageEnts.Select(x => ContainerGetBase(x, id)).Where(s => s != null).Select(s => s!);

    [CommandImplementation("id")]
    public IEnumerable<string> ContainerId([PipedArgument] IEnumerable<BaseContainer> containers) =>
        containers.Select(x => x.ID);


    public BaseContainer? ContainerGetBase(EntityUid ent, string id)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(ent, id, out var container))
            return null;

        return container;
    }

    [CommandImplementation("insertmultiple")]
    public BaseContainer ContainerInsert([PipedArgument] BaseContainer container, bool doForce, IEnumerable<EntityUid> ents)
    {
        _container ??= GetSys<SharedContainerSystem>();

        foreach (var ent in ents)
        {
            if (doForce)
            {
                _container.Insert(ent, container, null, true);
            }
            else
            {
                _container.InsertOrDrop(ent, container);
            }
        }
        return container;
    }

    [CommandImplementation("insert")]
    public BaseContainer ContainerInsert([PipedArgument] BaseContainer container, bool doForce, EntityUid ent)
    {
        return ContainerInsert(container, doForce, [ent]);
    }

    [CommandImplementation("list")]
    public IEnumerable<string> ContainerList([PipedArgument] EntityUid ent)
    {
        _container ??= GetSys<SharedContainerSystem>();

        return _container.GetAllContainers(ent).Select(container => container.ID);
    }
    [CommandImplementation("getall")]
    public IEnumerable<BaseContainer> ContainerGetAll([PipedArgument] EntityUid ent)
    {
        _container ??= GetSys<SharedContainerSystem>();

        return _container.GetAllContainers(ent);
    }

#region Starlight
    #region insert implementations

    [CommandImplementation("insert")]
    public EntityUid ContainerInsert([PipedArgument] EntityUid target, string containerId, EntityUid uid)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        _container.InsertOrDrop(uid, container);
        return target;
    }

    [CommandImplementation("insert")]
    public IEnumerable<EntityUid> ContainerInsert([PipedArgument] IEnumerable<EntityUid> target, string containerId, EntityUid uid) =>
        target.Select(x => ContainerInsert(x, containerId, uid));

    [CommandImplementation("insertmany")]
    public IEnumerable<EntityUid> ContainerInsertMany([PipedArgument] IEnumerable<EntityUid> entities, string containerId,
        EntityUid target) =>
        entities.Select(entity => ContainerInsert(target, containerId, entity));
    #endregion

    #region create implementations

    [CommandImplementation("create")]
    public EntityUid Create([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        _container.MakeContainer<Container>(target, containerId);
        return target;
    }

    [CommandImplementation("create")]
    public IEnumerable<EntityUid> Create([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.Select(x => Create(x, containerId));

    #endregion

    #region delete implementations

    [CommandImplementation("delete")]
    public EntityUid Delete([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        _container.ShutdownContainer(container);
        return target;
    }

    [CommandImplementation("delete")]
    public void Delete([PipedArgument] BaseContainer container)
    {
        _container ??= GetSys<SharedContainerSystem>();
        _container.ShutdownContainer(container);
    }

    [CommandImplementation("delete")]
    public IEnumerable<EntityUid> Delete([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.Select(x => Delete(x, containerId));

    #endregion

    #region drop implementation

    [CommandImplementation("drop")]
    public EntityUid Drop([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        _container.EmptyContainer(container);
        return target;
    }

    [CommandImplementation("drop")]
    public BaseContainer Drop([PipedArgument] BaseContainer container)
    {
        _container ??= GetSys<SharedContainerSystem>();
        _container.EmptyContainer(container);
        return container;
    }

    [CommandImplementation("dropandget")]
    public IEnumerable<EntityUid> DropGetEntities([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        return _container.EmptyContainer(container);
    }

    [CommandImplementation("dropandget")]
    public IEnumerable<EntityUid>? DropGetEntities([PipedArgument] BaseContainer container)
    {
        _container ??= GetSys<SharedContainerSystem>();
        return _container.EmptyContainer(container);
    }

    [CommandImplementation("dropanddelete")]
    public EntityUid DropAndDelete([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        _container.ShutdownContainer(container);
        return target;
    }

    [CommandImplementation("dropanddelete")]
    public void DropAndDelete([PipedArgument] BaseContainer container)
    {
        _container ??= GetSys<SharedContainerSystem>();
        _container.ShutdownContainer(container);
    }

    [CommandImplementation("drop")]
    public IEnumerable<EntityUid> Drop([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.Select(x => Drop(x, containerId));

    [CommandImplementation("dropandget")]
    public IEnumerable<EntityUid> DropGetEntities([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.SelectMany(x=>DropGetEntities(x, containerId));

    [CommandImplementation("dropanddelete")]
    public IEnumerable<EntityUid> DropAndDelete([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.Select(x => DropAndDelete(x, containerId));

    #endregion

    #region get implementations

    [CommandImplementation("get")]
    public BaseContainer Get([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        return _container.GetContainer(target, containerId);
    }

    [CommandImplementation("getentities")]
    public IEnumerable<EntityUid> GetEntities([PipedArgument] EntityUid target, string containerId)
    {
        _container ??= GetSys<SharedContainerSystem>();
        var container = _container.GetContainer(target, containerId);
        return container.ContainedEntities;
    }

    [CommandImplementation("getcontaining")]
    public IEnumerable<BaseContainer> GetContaining([PipedArgument] EntityUid target)
    {
        _container ??= GetSys<SharedContainerSystem>();
        List<BaseContainer> containers = [];
        containers.AddRange(_container.GetContainingContainers(target).Select(container => container));
        return containers;
    }

    [CommandImplementation("getoutercontainer")]
    public BaseContainer GetOuterContainer([PipedArgument] EntityUid target)
    {
        _container ??= GetSys<SharedContainerSystem>();
        return _container.GetContainingContainers(target).Last();
    }

    [CommandImplementation("getowner")]
    public EntityUid? GetOwner([PipedArgument] BaseContainer container) => container.Owner;

    [CommandImplementation("getentities")]
    public IEnumerable<EntityUid> GetEntities([PipedArgument] IEnumerable<EntityUid> target, string containerId) =>
        target.SelectMany(x => GetEntities(x, containerId));

    [CommandImplementation("getoutercontainer")]
    public IEnumerable<BaseContainer> GetOuterContainer([PipedArgument] IEnumerable<EntityUid> target) =>
        target.Select(GetOuterContainer);

    [CommandImplementation("getowner")]
    public IEnumerable<EntityUid?> GetOwner([PipedArgument] IEnumerable<BaseContainer> container) =>
        container.Select(GetOwner);

    #endregion
#endregion Starlight
}
