using Content.Shared._FarHorizons.CCVar;
using Content.Shared.Chat;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private bool _showRadioPrefix;

    private void InitializeFH() => 
        _config.OnValueChanged(FHCCVars.ChatShowFactionPrefix, (value) => _showRadioPrefix = value, true); // Far Horizons

    private void ProcessRadioPrefix(ref ChatMessage msg) => 
        msg.WrappedMessage = _showRadioPrefix && msg.RadioPrefix != null
            ? msg.WrappedMessage.Replace("[radioPrefix/]", $"({msg.RadioPrefix})")
            : msg.WrappedMessage.Replace("[radioPrefix/]", "");

    public void UpdateRadioPrefix(bool value)
    {
        _config.SetCVar(FHCCVars.ChatShowFactionPrefix, value);
        _config.SaveToFile();
    }
}