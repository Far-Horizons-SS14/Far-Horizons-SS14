using System.Threading;
using Content.Shared.Preferences;

namespace Content.Server._Starlight.TextToSpeech;

public interface ITTSClient
{
    // Far Horizons edit
    IAsyncEnumerable<byte[]> GenerateTTS(string text, Symspeech symspeech, TTSEffect effect = TTSEffect.None, CancellationToken cancellationToken = default);
    void Initialize();
}