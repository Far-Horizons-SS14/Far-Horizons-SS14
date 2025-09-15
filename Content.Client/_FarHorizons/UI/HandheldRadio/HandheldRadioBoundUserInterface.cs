using Content.Shared.FarHorizons.Tools.HandheldRadio;
using Content.Shared.FarHorizons.Tools.HandheldRadio.Components;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.UI.HandheldRadio
{
    public sealed class HandheldRadioBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        [ViewVariables]
        private HandheldRadioWindow? _window;

        public HandheldRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<HandheldRadioWindow>();
            _window.Title = _entManager.GetComponent<MetaDataComponent>(Owner).EntityName;

            if (_entManager.TryGetComponent(Owner, out HandheldRadioComponent? radio))
            {
                _window.Populate((float)Math.Round(radio!.CurrentFrequency, 1), radio!.FrequencyMin, radio!.FrequencyMax, radio!.MicEnabled, radio!.SpeakerEnabled);
            }

            _window.OnChangeFrequency += OnChangeFrequency;
            _window.OnMicrophoneToggled += OnMicrophoneToggled;
            _window.OnSpeakerToggled += OnSpeakerToggled;
        }

        private void OnChangeFrequency(float frequency) {
            if (_window == null ||
                !_entManager.TryGetComponent(Owner, out HandheldRadioComponent? radio) ||
                frequency > radio.FrequencyMax ||
                frequency < radio.FrequencyMin)
                return;
            
            SendPredictedMessage(new HandheldRadioFrequencyChange(frequency));
        }

        private void OnMicrophoneToggled(bool state) {
            if (_window == null ||
                !_entManager.TryGetComponent(Owner, out HandheldRadioComponent? radio))
                return;

            SendPredictedMessage(new HandheldRadioStateChange(HanheldRadioState.Microphone, state));
        }

        private void OnSpeakerToggled(bool state) {
            if (_window == null ||
                !_entManager.TryGetComponent(Owner, out HandheldRadioComponent? radio))
                return;

            SendPredictedMessage(new HandheldRadioStateChange(HanheldRadioState.Speaker, state));
        }

    }
}
