using Content.Shared._FarHorizons.CCVar;
using Robust.Shared.Configuration;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed partial class ChannelFilterPopup
{
    public event Action<bool>? OnShowRadioPrefixChanged;

    private void InitializeFHOptions(IConfigurationManager cfg)
    {
        FHOptionsShowPrefixes.Pressed = cfg.GetCVar(FHCCVars.ChatShowFactionPrefix);
        FHOptionsShowPrefixes.OnPressed += RadioPrefixChanged;
    }

    private void RadioPrefixChanged(ButtonEventArgs args) => 
        OnShowRadioPrefixChanged?.Invoke(args.Button.Pressed);
}