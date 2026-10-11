using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared._FarHorizons.Medical.Limbs;
using Content.Shared.Starlight;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.Medical.Limbs;

[UsedImplicitly]
public sealed partial class CyberLimbSystemItemsRadial(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private Dictionary<EntityUid, RadialMenuOptionBase> _buttons = new();
    private static readonly Color _selectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color _selectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _buttons.Clear();
        CreateButtons();
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (_menu == null)
            return;

        if (message is not RefreshLimbUIMessage msg)
            return;

        UpdateButton(msg.Ent, msg.Value);
    }

    private void UpdateButton(NetEntity ent, bool value)
    {
        var entity = EntMan.GetEntity(ent);
        if (!_buttons.TryGetValue(entity, out var button)
        || _menu == null)
            return;

        button.BackgroundColor =  value ? _selectedOptionBackground : null;
        button.HoverBackgroundColor = value ? _selectedOptionHoverBackground : null;
        _menu.UpdateBackgroundColors(_buttons.Values);
    }
    private void CreateButtons()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent<LimbItemStorageComponent>(Owner, out var lisComp) || !EntMan.TryGetComponent<LimbItemDeployerComponent>(Owner, out var lidComp))
            return;

        var models = ConvertToButtons((Owner, lisComp, lidComp));

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(Entity<LimbItemStorageComponent, LimbItemDeployerComponent> ent)
    {
        var ToggleOption = new RadialMenuActionOption<NetEntity>(SendLimbToggle, EntMan.GetNetEntity(ent.Owner))
        {
            IconSpecifier = RadialMenuIconSpecifier.With(
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png"))
            ),
            ToolTip = ent.Comp2.Toggled ? "Undeploy Limb" : "Deploy Limb",
            BackgroundColor = ent.Comp2.Toggled ? _selectedOptionBackground : null,
            HoverBackgroundColor = ent.Comp2.Toggled ? _selectedOptionHoverBackground : null
        };
        _buttons.Add(ent.Owner, ToggleOption);

        foreach (var item in ent.Comp1.ItemEntities)
        {
            if (!EntMan.TryGetComponent<MetaDataComponent>(item.Key, out var metadata)
            || metadata.EntityPrototype is not { } proto)
                continue;

            var option = new RadialMenuActionOption<NetEntity>(SendLimbItemToggle, EntMan.GetNetEntity(item.Key))
            {
                IconSpecifier = RadialMenuIconSpecifier.With((EntProtoId) proto.ID),
                ToolTip = metadata.EntityName,
                BackgroundColor = item.Value ? _selectedOptionBackground : null,
                HoverBackgroundColor = item.Value ? _selectedOptionHoverBackground : null,
                KeepOpen = true
            };
            _buttons.Add(item.Key, option);
        }

        return _buttons.Values;
    }

    private void SendLimbToggle(NetEntity _) 
        => SendPredictedMessage(new LimbToggleMessage());

    private void SendLimbItemToggle(NetEntity item)
        => SendPredictedMessage(new LimbItemToggleMessage(item));
}
