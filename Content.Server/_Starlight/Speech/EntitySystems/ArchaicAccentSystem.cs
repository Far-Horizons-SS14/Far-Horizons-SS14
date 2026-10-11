using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class ArchaicAccentSystem : RelayAccentSystem<ArchaicAccentComponent>
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    protected override SpeechMessage AccentuateInternal(EntityUid uid, ArchaicAccentComponent component, SpeechMessage message)
        => _replacement.ApplyReplacements(message, "archaic");
}
