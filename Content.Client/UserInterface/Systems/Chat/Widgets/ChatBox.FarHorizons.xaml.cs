namespace Content.Client.UserInterface.Systems.Chat.Widgets;

public partial class ChatBox
{
    private void InitializeFHOptions() => 
        ChatInput.FilterButton.Popup.OnShowRadioPrefixChanged += OnShowRadioPrefixChanged;

    private void OnShowRadioPrefixChanged(bool value) => 
        _controller.UpdateRadioPrefix(value);
}