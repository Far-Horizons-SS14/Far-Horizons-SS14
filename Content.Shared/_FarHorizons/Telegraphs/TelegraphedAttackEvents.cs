namespace Content.Shared._FarHorizons.Telegraphs;

[ByRefEvent]
public record struct OnTelegraphTriggered(int Telegraph);

[ByRefEvent]
public record struct OnTelegraphAttackFinished(int Telegraph);