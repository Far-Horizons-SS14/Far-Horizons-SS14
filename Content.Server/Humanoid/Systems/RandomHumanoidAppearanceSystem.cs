using Content.Server.Humanoid.Components;
using Content.Shared.Body;
using Content.Shared.Humanoid;
// FarHorizons Start
using Content.Shared.Preferences;
using Robust.Shared.Prototypes; 
using System.Linq; 
using Content.Server.Cloning;
using Content.Server.Body.Components;
using Content.Server.Body;
// FarHorizons End

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // FarHorizons
    [Dependency] private readonly CloningSystem _cloningSystem = default!; // FarHorizons

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedVisualBodySystem)]); //FarHorizons
        SubscribeLocalEvent<RandomSpeciesComponent, ComponentStartup>(OnCompStartSpecies, before: [typeof(InitialBodySystem)]); //FarHorizons
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);

        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);
        _visualBody.MatchMarkingsToSkinColorAndRandomHair(uid, profile); //FarHorizons

        //FarHorizons Start
        var name = profile.Name; 
        if(component.lastNameOnly)
        {
            name = name.Split(" ").Last();
        }
        if (component.RandomizeName)
            _metaData.SetEntityName(uid, $"{component.namePrefix} {name}");
        //FarHorizons End
    }

    // FarHorizons Start
    private void OnCompStartSpecies(EntityUid uid, RandomSpeciesComponent component, ComponentStartup args)
    {
        if (!HasComp<HumanoidProfileComponent>(uid))
            return;
        var profile = HumanoidCharacterProfile.Random();
        var speciesProto = _prototypeManager.Index(profile.Species);
        
        var dummy = Spawn(speciesProto.Prototype);
        if (!_prototypeManager.Resolve(component.TransformCloningSettings, out var settings))
            return;

        _visualBody.CopyAppearanceFrom(dummy, uid);
        _cloningSystem.CloneComponents(dummy, uid, settings);
        _humanoidProfile.ApplyProfileTo(uid, profile);
        if(!HasComp<RespiratorComponent>(dummy))
            RemComp<RespiratorComponent>(uid);
        Del(dummy);
    }
    // FarHorizons End
}
