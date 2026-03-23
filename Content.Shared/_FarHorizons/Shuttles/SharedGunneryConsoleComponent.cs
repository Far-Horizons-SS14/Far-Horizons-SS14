using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Shuttles;

[Serializable, NetSerializable]
public enum GunneryConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GunneryConsoleBuiState : BoundUserInterfaceState
{
    public List<GunneryConsoleTurretEntry> TurretEntities = [];
    public List<NetEntity> Selected = [];
    public NavInterfaceState State = new(0f, null, null, []);
}

[Serializable, NetSerializable]
public struct GunneryConsoleTurretMetaData
{
    public string EntityName;
    public NetCoordinates Coordinates;

    public GunneryConsoleTurretMetaData(string name, NetCoordinates coordinates)
    {
        EntityName = name;
        Coordinates = coordinates;
    }
}

[Serializable, NetSerializable]
public struct GunneryConsoleTurretEntry
{
    public NetEntity NetEntity;
    public int CurrentAmmo;
    public int MaxAmmo;
    [NonSerialized] public GunneryConsoleTurretMetaData? MetaData = null;

    public GunneryConsoleTurretEntry(NetEntity netEntity, int currentAmmo, int maxAmmo)
    {
        NetEntity = netEntity;
        CurrentAmmo = currentAmmo;
        MaxAmmo = maxAmmo;
    }
}

[Serializable, NetSerializable]
public sealed class GunneryConsoleTargetActionMessage(Vector2? position) : BoundUserInterfaceMessage
{
    public Vector2? Position { get; } = position;
}

[Serializable, NetSerializable]
public sealed class GunneryConsoleSelectActionMessage(List<(NetEntity, bool)> turretEntities) : BoundUserInterfaceMessage
{
    public List<(NetEntity ent, bool add)> TurretEntities { get; } = turretEntities;
}

[Serializable, NetSerializable]
public sealed class GunneryConsoleFireActionMessage(NetCoordinates position, List<NetEntity> turretEntities) : BoundUserInterfaceMessage
{
    public NetCoordinates Position { get; } = position;
    public List<NetEntity> TurretEntities { get; } = turretEntities;
}