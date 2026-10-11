using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;
using Content.Shared.Atmos;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing.EntitySystems; //FH

namespace Content.Shared._Starlight.CoolingUnit;

public abstract partial class SharedCoolingUnitSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CoolingUnitComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<CoolingUnitComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CoolingUnitComponent, ToggleActionEvent>(OnActionToggle);
    }

    private void OnGetActions(EntityUid uid, CoolingUnitComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnExamined(EntityUid uid, CoolingUnitComponent component, ExaminedEvent args)
    {
        if (_itemToggle.IsActivated(uid))
            args.PushMarkup(Loc.GetString("coolingunit-on-examine"));
        else
            args.PushMarkup(Loc.GetString("coolingunit-off-examine"));
    }

    private void OnActionToggle(Entity<CoolingUnitComponent> entity, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        _itemToggle.Toggle(entity.Owner, args.Performer);
        args.Handled = true;
    }

    //FH Start
    [SubscribeLocalEvent]
    private void OnItemToggled(Entity<CoolingUnitComponent> ent, ref ItemToggledEvent args)
    {
        _appearance.SetData(ent, CoolingUnitVisuals.Enabled, args.Activated);
        _clothing.SetEquippedPrefix(ent, args.Activated ? "on" : null);
    }

    [SubscribeLocalEvent]
    private void OnToggleMessage(Entity<CoolingUnitComponent> ent, ref CoolingUnitToggleMessage args)
        => _itemToggle.Toggle(ent.Owner, args.Actor);

    [SubscribeLocalEvent]
    private void OnChangeTemperature(Entity<CoolingUnitComponent> ent, ref CoolingUnitChangeTemperatureMessage args)
    {
        ent.Comp.DesiredTemp = MathF.Max(args.Temperature, ent.Comp.MinTemperature);
        ent.Comp.DesiredTemp = MathF.Max(ent.Comp.DesiredTemp.Value, Atmospherics.TCMB);
        Dirty(ent);
    }
    //FH End
}