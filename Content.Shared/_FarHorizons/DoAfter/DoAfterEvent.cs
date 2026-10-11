
namespace Content.Shared.DoAfter;

[ByRefEvent]
public record struct BeforeDoAfterEvent(DoAfterArgs Args, bool Cancelled = false);