using Content.Shared.Access;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Factions;

/// <summary>
///     A prototype for creating alternative versions of base game departments for Far Horizons factions
/// </summary>
[Prototype]
public sealed partial class FactionDepartmentAssignmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("faction", required: true)]
    public ProtoId<FactionPrototype> Faction = default!;

    [DataField("department", required: true)]
    public ProtoId<DepartmentPrototype> Department = default!;

    [DataField("weight")]
    public int Weight { get; private set; }

    [DataField("nameOverride")]
    public LocId? NameOverride = null;

    [DataField("descriptionOverride")]
    public LocId? DescriptionOverride = null;

    [DataField("colorOverride")]
    public Color? ColorOverride;
}

/// <summary>
///     A prototype for creating alternative versions of base game jobs for Far Horizons factions
/// </summary>
[Prototype]
public sealed partial class FactionJobAssignmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("faction", required: true)]
    public ProtoId<FactionPrototype> Faction = default!;

    [DataField("job", required: true)]
    public ProtoId<JobPrototype> Job = default!;

    [DataField("weight")]
    public int Weight { get; private set; }

    [DataField("override")]
    public JobOverride? Override = default!;
}

/// <summary>
///     Data field, storing values that FactionJobAssignment will override on base job
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class JobOverride{
    [DataField]
    public string? Name;
    [DataField]
    public string? Description;
    [DataField]
    public ProtoId<JobIconPrototype>? Icon;
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;
    [DataField]
    public ProtoId<RoleLoadoutPrototype>? Loadout;
    [DataField]
    public EntProtoId? JobEntity;
    [DataField]
    public EntProtoId? JobPreviewEntity;
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>>? Access;
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessGroupPrototype>>? AccessGroups;
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>>? ExtendedAccess;
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessGroupPrototype>>? ExtendedAccessGroups;

}