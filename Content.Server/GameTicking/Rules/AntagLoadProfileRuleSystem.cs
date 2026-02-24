using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared.Body;
using Content.Server.Traits;
using Content.Shared._Starlight.Character.Info;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!; // Starlight
    [Dependency] private readonly TraitSystem _traitSystem = default!; //Starlight
    [Dependency] private readonly SLSharedCharacterInfoSystem _sLSharedCharacterInfoSystem = default!; //Starlight

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        // Try to find a profile with this antagonist enabled on the player preferences
        HumanoidCharacterProfile? profile = null;
        if (args.Session != null)
        {
            var roles = args.AntagRoles;
            var prefs = _prefs.GetPreferences(args.Session.UserId);
            profile = prefs.SelectProfileForAntag(roles);
        }

        // Startlight - Start (Changing fully so RandomWithSpecies loads with a specieID)
        var species = _proto.Index(HumanoidCharacterProfile.DefaultSpecies);
        if (profile is not null)
            species = _proto.Index(profile.Species);

        if (ent.Comp.SpeciesHardOverride is not null)
            species = _proto.Index(ent.Comp.SpeciesHardOverride.Value);
        else if (ent.Comp.SpeciesOverride is not null
            && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);

        if (profile is null)
            profile = HumanoidCharacterProfile.RandomWithSpecies(species.ID);

        args.Entity = Spawn(species.Prototype);
        if (profile?.WithSpecies(species.ID) is { } humanoidProfile)
        {
            _visualBody.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            _humanoidProfile.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            _sLSharedCharacterInfoSystem.ApplyCharacterInfo(args.Entity.Value, profile); // Starlight
        }
    }
}
