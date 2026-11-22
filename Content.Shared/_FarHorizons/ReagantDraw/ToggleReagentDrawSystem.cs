using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._FarHorizons.ReagentDraw;

/// <summary>
/// Handles events to integrate ReagentDraw with ItemToggle
/// </summary>
public sealed partial class ToggleReagentDrawSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedReagentDrawSystem _reagent = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleReagentDrawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleReagentDrawComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ToggleReagentDrawComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ToggleReagentDrawComponent, ReagentContainerSlotEmptyEvent>(OnEmpty);
    }

    private void OnMapInit(Entity<ToggleReagentDrawComponent> ent, ref MapInitEvent args) 
        => _reagent.SetDrawEnabled(ent.Owner, _toggle.IsActivated(ent.Owner));

    private void OnActivateAttempt(Entity<ToggleReagentDrawComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_reagent.HasDrawReagent(ent.Owner))
            args.Cancelled = true;
    }

    private void OnToggled(Entity<ToggleReagentDrawComponent> ent, ref ItemToggledEvent args)
        => _reagent.SetDrawEnabled(ent.Owner, args.Activated);

    private void OnEmpty(Entity<ToggleReagentDrawComponent> ent, ref ReagentContainerSlotEmptyEvent args) 
        => _toggle.TryDeactivate(ent.Owner);
}
