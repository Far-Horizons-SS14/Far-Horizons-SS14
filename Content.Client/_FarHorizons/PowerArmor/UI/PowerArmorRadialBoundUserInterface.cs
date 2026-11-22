using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared._FarHorizons.PowerArmor;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.PowerArmor.UI;

[UsedImplicitly]
public sealed partial class PowerArmorRadialBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private static readonly Color _selectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color _selectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        Update();
        _menu.OpenOverMouseScreenPosition();
    }

    public override void Update()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent<PowerArmorComponent>(Owner, out var PAComp))
            return;

        var models = ConvertToButtons((Owner, PAComp));

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(Entity<PowerArmorComponent> ent)
    {
        var buttons = new List<RadialMenuOptionBase>();

        var ToggleOption = new RadialMenuActionOption<NetEntity>(SendArmorToggle, EntMan.GetNetEntity(ent.Owner))
        {
            IconSpecifier = RadialMenuIconSpecifier.With(
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png"))
            ),
            ToolTip = ent.Comp.IsPowered ? "Deactivate Power Armor" : "Activate Power Armor",
            BackgroundColor = ent.Comp.IsPowered ? _selectedOptionBackground : null,
            HoverBackgroundColor = ent.Comp.IsPowered ? _selectedOptionHoverBackground : null
        };
        buttons.Add(ToggleOption);

        foreach (var module in ent.Comp.Modules)
        {
            if (!EntMan.TryGetComponent<PowerArmorModuleComponent>(module, out var PAMComp)
            || !PAMComp.canBeToggled
            || !EntMan.TryGetComponent<MetaDataComponent>(module, out var metadata))
                continue;

            var option = new RadialMenuActionOption<NetEntity>(SendModuleToggle, EntMan.GetNetEntity(module))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(module),
                ToolTip = metadata.EntityName,
                BackgroundColor = PAMComp.isEnabled ? _selectedOptionBackground : null,
                HoverBackgroundColor = PAMComp.isEnabled ? _selectedOptionHoverBackground : null
            };
            buttons.Add(option);
        }

        return buttons;
    }

    private void SendModuleToggle(NetEntity moduleEntity) 
        => SendPredictedMessage(new PowerArmorToggleModuleMessage(moduleEntity));

    private void SendArmorToggle(NetEntity _) 
        => SendPredictedMessage(new TogglePowerArmorMessage());
}
