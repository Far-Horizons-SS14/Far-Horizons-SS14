using Robust.Shared.Prototypes;

namespace Content.Shared.FarHorizons.Factions;

/// <summary>
///     A faction, such as NT or Syndicate
/// </summary>
[Prototype]
public sealed partial class FactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name = "????";

    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Alternative names by which this faction might be called (currently only in 'setfaction' console command)
    /// </summary>
    [DataField("alias")]
    public string[] Alias = [];

    /// <summary>
    /// There should always exist at least 1 default faction. Without it, things will break. 
    /// If multiple factions marked as default, the one with the lowest weight will be selected.
    /// </summary>
    [DataField("default")]
    public bool Default = false;

    [DataField("color")]
    public string Color = "white";

    [DataField("playable")]
    public bool Playable = false;

    /// <summary>
    /// Only one Major faction can exist in a round. This is the faction that round lobby selects before round starts.
    /// </summary>
    [DataField("major")]
    public bool Major = false;

    [DataField("description")]
    public string Description = "";

    /// <summary>
    /// Ordering in UI, or any other place where ordering might matter. Lower means higher in priority.
    /// </summary>
    [DataField("weight")]
    public int Weight { get; private set; }

}