using Content.Server._Starlight.Traits;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Server.Traits;
using Content.Shared._Starlight.Character.Info;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private TraitSystem _traitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        HumanoidCharacterProfile? profile = null;
        if (args.Session != null)
        {
            var prefs = _prefs.GetPreferences(args.Session.UserId);
            profile = prefs.SelectProfileForAntag(args.Antag.PrefRoles);
        }

        var species = _proto.Index(HumanoidCharacterProfile.DefaultSpecies);
        if (profile is not null)
            species = _proto.Index(profile.Species);

        if (ent.Comp.SpeciesHardOverride is not null)
            species = _proto.Index(ent.Comp.SpeciesHardOverride.Value);
        else if (ent.Comp.SpeciesOverride is not null
                 && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
        {
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);
        }

        profile ??= HumanoidCharacterProfile.RandomWithSpecies(species.ID);

        args.Entity = Spawn(species.Prototype, args.Coords);
        if (profile.WithSpecies(species.ID) is { } humanoidProfile)
        {
            _visualBody.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            _humanoidProfile.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            if (args.Session is not null)
                _traitSystem.ApplyTraits(args.Entity.Value, profile, args.Session);
        }
    }
}
