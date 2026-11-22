using Content.Shared.Actions;

namespace Content.Shared._FarHorizons.CartridgeLoader.Cartridges;
public sealed partial class MedTekActionEvent : EntityTargetActionEvent;
public sealed partial class HullSenseActionEvent : EntityTargetActionEvent;
public sealed partial class CrimeCheckActionEvent : EntityTargetActionEvent;
[ByRefEvent]
public record struct CheckCriminalEvent(EntityUid User, EntityUid Target);