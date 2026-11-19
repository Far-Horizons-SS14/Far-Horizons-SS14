using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.Network;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryChangeAppearance : ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (!entMan.TryGetComponent<HumanoidProfileComponent>(target, out var humanoid))
            return;
        
        if (IoCManager.Resolve<INetManager>().IsClient)
            return;

        var visBody = entMan.System<SharedVisualBodySystem>();
        
        var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        visBody.ApplyProfileTo(target, newProfile);
        visBody.MatchMarkingsToSkinColorAndRandomHair(target, newProfile);
        entMan.System<HumanoidProfileSystem>().ApplyProfileTo(target, newProfile);
        entMan.System<MetaDataSystem>().SetEntityName(target, newProfile.Name, raiseEvents: false);
        entMan.System<IdentitySystem>().QueueIdentityUpdate(target);
    }
}