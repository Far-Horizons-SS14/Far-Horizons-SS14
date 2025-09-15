using Robust.Shared.Audio;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Content.Server.FarHorizons.Tools.Shipyard.Systems;

namespace Content.Server.FarHorizons.Tools.Shipyard.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(IntegrityAnalyzerSystem))]
public sealed partial class IntegrityAnalyzerComponent : Component
{
    /// <summary>
    /// When should the next update be sent for the scanned entity
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between scan updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long it takes to scan an object.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Which entity has been scanned, for continuous updates
    /// </summary>
    [DataField]
    public EntityUid? ScannedEntity;

    /// <summary>
    /// The maximum range in tiles at which the analyzer can receive continuous updates, a value of null will be infinite range
    /// </summary>
    [DataField]
    public float? MaxScanRange = 2.5f;

    /// <summary>
    /// Sound played on scanning begin
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    /// Sound played on scanning end
    /// </summary>
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Whether to show up the popup
    /// </summary>
    [DataField]
    public bool Silent;
    
    [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
    public List<string>? DamageContainers;

}
