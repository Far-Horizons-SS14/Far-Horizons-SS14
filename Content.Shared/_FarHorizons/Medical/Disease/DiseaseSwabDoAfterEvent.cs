using Content.Shared.DoAfter;
using Content.Shared.Medical.Disease.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Event for the <see cref="DiseaseSwabSystem"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DiseaseSwabDoAfterEvent : SimpleDoAfterEvent;
