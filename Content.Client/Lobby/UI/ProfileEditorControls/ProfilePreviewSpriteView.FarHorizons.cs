using Content.Shared.Body;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    private void LoadCybernetics(HumanoidCharacterProfile humanoid)
    {
        if (humanoid.Cybernetics.Count <= 0 || 
            !EntMan.TryGetComponent<BodyComponent>(PreviewDummy, out var body) || 
            body.Organs == null) return;

        Dictionary<ProtoId<OrganCategoryPrototype>, List<EntityUid>> organMap = [];

        foreach (var organEnt in body.Organs.ContainedEntities)
        {
            if (!EntMan.TryGetComponent<OrganComponent>(organEnt, out var organ) || organ.Category == null) continue;

            if (!organMap.TryGetValue(organ.Category.Value, out var list))
            {
                list = new List<EntityUid>();
                organMap[organ.Category.Value] = list;
            }
            list.Add(organEnt);
        }

        foreach (var cybernetic in humanoid.Cybernetics)
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(cybernetic, out var cyberneticEntProto) ||
                !cyberneticEntProto.Components.TryGetComponent("Organ", out var rawComp) ||
                rawComp is not OrganComponent cyberneticComp ||
                cyberneticComp.Category == null) continue;

            if (organMap.TryGetValue(cyberneticComp.Category.Value, out var toDelete))
                foreach (var ent in toDelete)
                    EntMan.PredictedDeleteEntity(ent);

            var spawned = EntMan.Spawn(cybernetic);
            _container.Insert(spawned, body.Organs);
        }
    }
}