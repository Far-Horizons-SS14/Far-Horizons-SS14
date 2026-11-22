using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Content.Shared._Starlight.Cybernetics.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Content.Shared._FarHorizons.Medical.Limbs; //FH

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem
{
    [SubscribeLocalEvent]
    private void OnToggleLimbMessage(Entity<LimbItemDeployerComponent> ent, ref LimbToggleMessage args)
        => LimbToggle(ent, args.Actor);

    [SubscribeLocalEvent]
    private void OnToggleLimbItemMessage(Entity<LimbItemStorageComponent> ent, ref LimbItemToggleMessage args)
    {
        var item = GetEntity(args.Item);
        if(!ent.Comp.ItemEntities.TryGetValue(item, out var value) 
        || !TryComp<LimbItemDeployerComponent>(ent.Owner, out var lidComp))
            return;

        ent.Comp.ItemEntities[item] = !value;
        Dirty(ent);
        _uiSystem.ServerSendUiMessage(ent.Owner, LimbItemToggleMenuUiKey.Key, new RefreshLimbUIMessage(GetNetEntity(item), ent.Comp.ItemEntities[item]));

        if(lidComp.Toggled && ent.Comp.ItemEntities[item])
        {
            var handId = $"{ent.Owner}_{item}";
            var hands = EnsureComp<HandsComponent>(args.Actor);
            _hands.AddHand((args.Actor, hands), handId, HandLocation.Functional, whitelist: lidComp.HandWhitelist);
            _hands.DoPickup(args.Actor, handId, item, hands);
            EnsureComp<UnremoveableComponent>(item);
            if (ent.Comp.ItemEntities.Count == 1)
                _hands.SetActiveHand((args.Actor, hands), handId);
        }
        else if(!ent.Comp.ItemEntities[item])
        {
            var container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId, out _);
            var handId = $"{ent.Owner}_{item}";
            RemComp<UnremoveableComponent>(item);
            _container.Insert(_slEnt.Entity<TransformComponent, MetaDataComponent, PhysicsComponent>(item), container, force: true);
            _hands.RemoveHand(args.Actor, handId);
        }

        if(lidComp.Toggled)
            _audio.PlayPvs(lidComp.Sound, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnLimbItemToggleMenu(Entity<LimbItemDeployerComponent> ent, ref ToggleCyberlimbMenuEvent args)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp) 
        || HasComp<CyberneticDisruptionComponent>(args.Performer)
        || !TryComp<LimbItemStorageComponent>(ent, out var lisComp))
            return;

        if(lisComp.ItemEntities.Count == 1)
        {
            LimbToggle(ent, args.Performer);
            return;
        }

        if (!_uiSystem.IsUiOpen((ent, userInterfaceComp), LimbItemToggleMenuUiKey.Key, args.Performer))
            _uiSystem.OpenUi((ent, userInterfaceComp), LimbItemToggleMenuUiKey.Key, args.Performer);
        else
        {
            _uiSystem.CloseUi((ent, userInterfaceComp), LimbItemToggleMenuUiKey.Key, args.Performer);
            LimbToggle(ent, args.Performer);
        }
    }
}
