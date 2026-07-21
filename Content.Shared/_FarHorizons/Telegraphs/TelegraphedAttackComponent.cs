using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Telegraphs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TelegraphedAttackComponent : Component
{
    [ViewVariables, AutoNetworkedField] public TimeSpan StartTime = TimeSpan.Zero;
    [ViewVariables, AutoNetworkedField] public bool IsActive;
    [ViewVariables] public List<int> TriggeredParts = [];
    [ViewVariables] public TimeSpan ContinuousEffectNextRefresh = TimeSpan.Zero;
    [ViewVariables] public List<EntityUid> IgnoreTargets = [];
    [DataField] public TelegraphKind Kind = TelegraphKind.Hostile;
    [DataField] public List<AttackTelegraph> Telegraphs = [];
    [DataField] public bool ActiveOnSpawn = true;
    [DataField] public bool DeleteOnComplete = true;
    [DataField] public bool Continuous;
    [DataField] public TimeSpan ContinuousEffectRefreshRate = TimeSpan.FromSeconds(1);
}

[DataDefinition]
public sealed partial class AttackTelegraph
{
    [DataField] public TimeSpan DisplayDelay = TimeSpan.Zero;
    [DataField(required: true)] public TimeSpan EffectDelay;
    [DataField] public HashSet<Vector2i> Tiles = [];
    [DataField] public ITelegraphedAttackEffect? Effect;
    [DataField] public bool Inverse;
}

public interface ITelegraphedAttackEffect
{
    public abstract void Run();
}

/// To add new kinds, you need:
/// 1. new enum value here
/// 2. new clientside cvar
/// 3. UI for setting this cvar in accessibility tab in settings
/// 4. Update SetColor() and GetColor() functions in TelegraphedAttackOverlay to use correct color when drawing telegraphs
/// 5. Update clientside system to read this cvar and send it to SetColor() on overlay
public enum TelegraphKind
{
    Hostile,
    Utility,
}