using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.NPC;

public static class SimpleBotBlackboard
{
    public const string SpeechEnabled = "SimpleBotSpeechEnabled";
}

public enum SimpleBotPatrolMode : byte
{
    Wander,
    Leash,
    Patrol,
}

/// <summary>
/// Stores the operator-configurable settings shared by the small service bots.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SimpleBotConfigurationComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Powered = true;

    [DataField, AutoNetworkedField]
    public bool SpeechEnabled = true;

    [DataField, AutoNetworkedField]
    public string PatrolBeacon = string.Empty;

    [DataField, AutoNetworkedField]
    public SimpleBotPatrolMode PatrolMode;
}

[Serializable, NetSerializable]
public sealed class SimpleBotConfigurationBuiMessage : BoundUserInterfaceMessage
{
    public bool Powered { get; }
    public bool SpeechEnabled { get; }
    public SimpleBotPatrolMode PatrolMode { get; }
    public string PatrolBeacon { get; }

    public SimpleBotConfigurationBuiMessage(bool powered, bool speechEnabled, SimpleBotPatrolMode patrolMode, string patrolBeacon)
    {
        Powered = powered;
        SpeechEnabled = speechEnabled;
        PatrolMode = patrolMode;
        PatrolBeacon = patrolBeacon;
    }
}

[Serializable, NetSerializable]
public enum SimpleBotConfigurationUiKey : byte
{
    Key,
}
