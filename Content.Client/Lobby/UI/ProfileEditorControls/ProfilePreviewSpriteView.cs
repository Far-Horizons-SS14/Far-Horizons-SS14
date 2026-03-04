using Content.Shared._FarHorizons.Factions;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private ISharedFactionManager _factions = default!; // Far Horizons
    [Dependency] private IClientPreferencesManager _preferencesManager = default!; // Far Horizons
    private ContainerSystem _container; // Far Horizons

    /// <summary>
    /// The name of the loaded profile
    /// </summary>
    public string? ProfileName { get; private set; }

    /// <summary>
    /// The name of the preferred job of the loaded profile, if any
    /// </summary>
    public string? JobName { get; private set; }

    /// <summary>
    /// The job loadout override name of the loaded profile, if any
    /// </summary>
    public string? LoadoutName { get; private set; }

    /// <summary>
    /// The profile name, loadout override name, and preferred job name formatted into lines for use in
    /// something like the <see cref="CharacterPickerButton"/>.
    /// </summary>
    public string? FullDescription { get; private set; }

    /// <summary>
    /// Entity used for the profile editor preview
    /// </summary>
    public EntityUid PreviewDummy;

    public ProfilePreviewSpriteView()
    {
        IoCManager.InjectDependencies(this);
        _container = EntMan.System<ContainerSystem>(); // Far Horizons
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
    public void LoadPreview(HumanoidCharacterProfile profile, (FactionPrototype, JobPrototype)? jobOverride = null, bool showClothes = true, ProtoId<AntagPrototype>? antagOverride = null)
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        LoadHumanoidEntity(profile, jobOverride, showClothes, antagOverride);

        FullDescription = ConstructFullDescription();

        SetEntity(PreviewDummy);
        SetName(profile.Name);
    }

    /// <summary>
    /// Sets the preview entity's name without reloading anything else.
    /// </summary>
    public void SetName(string newName)
    {
        EntMan.System<MetaDataSystem>().SetEntityName(PreviewDummy, newName);
    }

    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    public void ReloadProfilePreview(HumanoidCharacterProfile profile)
    {
        ReloadHumanoidEntity(profile);
    }

    public void ClearPreview()
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        ClearPreview();
    }

    private string ConstructFullDescription()
    {
        var descriptionLines = new List<string>();

        if (ProfileName != null)
            descriptionLines.Add(ProfileName);

        if (LoadoutName != null)
            descriptionLines.Add($"\"{LoadoutName}\"");

        if (JobName != null)
            descriptionLines.Add(JobName);

        return string.Join("\n", descriptionLines);
    }
}
