using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.StationEvents.Components;
using Content.Shared._Starlight.Language.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.Traits.Assorted;

namespace Content.Server._FarHorizons.Body;
public sealed partial class BrainTransferSystem : EntitySystem
{
    public void TransferMindComponents(EntityUid body, EntityUid brain)
    {
        if(!TryComp<BrainComponent>(brain, out var brainComp))
            return;
        Log.Info("weh");
    }

    private static readonly Type[] _mindComponents =
    {
        typeof(NPCRetaliationComponent),
        typeof(NpcFactionMemberComponent),
        typeof(GhostTakeoverAvailableComponent),
        typeof(GhostRoleComponent),
        typeof(ActiveNPCComponent),
        typeof(SentienceTargetComponent),
        typeof(HTNComponent),
        typeof(LanguageKnowledgeComponent),
        typeof(LanguageSpeakerComponent),
        typeof(ParacusiaComponent)
    };
}