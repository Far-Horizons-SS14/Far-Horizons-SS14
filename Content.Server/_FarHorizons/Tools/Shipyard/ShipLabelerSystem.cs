using Robust.Server.GameObjects;
using Content.Shared.FarHorizons.Tools.Shipyard;
using Content.Shared.FarHorizons.Tools.Shipyard.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.FarHorizons.Tools.Shipyard.Systems;

public sealed class ShipLabelerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipLabelerComponent, ShipLabelerNameChangeRequest>(OnNameChangeRequest);
    }

    private void OnNameChangeRequest(EntityUid uid, ShipLabelerComponent shipLabeler, ShipLabelerNameChangeRequest args){

        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) || transform.GridUid is null){
            _uiSystem.ServerSendUiMessage(uid, ShipLabelerUiKey.Key, new ShipLabelerNameChangeResponse(false, "No grid to edit!"));
            return;
        }

        if (!EntityManager.TryGetComponent(transform.GridUid, out MetaDataComponent? metadata) || metadata.EntityName == args.Name){
            _uiSystem.ServerSendUiMessage(uid, ShipLabelerUiKey.Key, new ShipLabelerNameChangeResponse(false, "New name is the same as old name!"));
            return;
        }

        if ((!EntityManager.TryGetComponent(transform.GridUid, out ShuttleComponent? shuttle) || !shuttle.Enabled) && !shipLabeler.NoChecks){
            _uiSystem.ServerSendUiMessage(uid, ShipLabelerUiKey.Key, new ShipLabelerNameChangeResponse(false, "Grid is not a shuttle!"));
            return;
        }

        _metaData.SetEntityName((EntityUid)transform.GridUid, args.Name);
        _uiSystem.ServerSendUiMessage(uid, ShipLabelerUiKey.Key, new ShipLabelerNameChangeResponse(true));
    }

}
