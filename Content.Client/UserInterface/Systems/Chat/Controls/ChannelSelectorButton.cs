using System.Numerics;
using Content.Shared.CollectiveMind;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Content.Shared._FarHorizons.CCVar;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : ChatPopupButton<ChannelSelectorPopup>
{
    public event Action<ChatSelectChannel>? OnChannelSelect;

    public ChatSelectChannel SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    private readonly IConfigurationManager _cfg; // Far Horizons

    public ChannelSelectorButton()
    {
        Name = "ChannelSelector";

        Popup.Selected += OnChannelSelected;

        if (Popup.FirstChannel is { } firstSelector)
        {
            Select(firstSelector);
        }

        _cfg = IoCManager.Resolve<IConfigurationManager>(); // Far Horizons
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
    }

    private void OnChannelSelected(ChatSelectChannel channel)
    {
        Select(channel);
    }

    public void Select(ChatSelectChannel channel)
    {
        if (Popup.Visible)
        {
            Popup.Close();
        }

        if (SelectedChannel == channel)
            return;
        SelectedChannel = channel;
        OnChannelSelect?.Invoke(channel);
    }

    public static string ChannelSelectorName(ChatSelectChannel channel)
    {
        return Loc.GetString($"hud-chatbox-select-channel-{channel}");
    }

    public Color ChannelSelectColor(ChatSelectChannel channel)
    {
        return channel switch
        {
            ChatSelectChannel.Radio => Color.LimeGreen,
            ChatSelectChannel.LOOC => Color.MediumTurquoise,
            ChatSelectChannel.OOC => Color.LightSkyBlue,
            ChatSelectChannel.Dead => Color.MediumPurple,
            ChatSelectChannel.Admin => Color.HotPink,
            ChatSelectChannel.Mentor => Color.DarkViolet, // Far Horizons
            _ => Color.DarkGray
        };
    }

    public void UpdateChannelSelectButton(ChatSelectChannel channel, Shared.Radio.RadioChannelPrototype? radio, CollectiveMindPrototype? collectiveMind)
    {
        if (radio != null)
        {
            // Far Horizons start
            var name = Loc.GetString(radio.Name);
            if (_cfg.GetCVar(FHCCVars.ChatShowFactionPrefix) && radio.ChatPrefix != null)
                name = $"({radio.ChatPrefix}){name}";
            
            Text = name;
            // Far Horizons end
            Modulate = radio?.Color ?? ChannelSelectColor(channel);
        }
        else if (collectiveMind != null)
        {
            Text = Loc.GetString(collectiveMind.Name);
            Modulate = collectiveMind.Color;
        }
        else
        {
            Text = ChannelSelectorName(channel);
            Modulate = ChannelSelectColor(channel);
        }
    }
}
