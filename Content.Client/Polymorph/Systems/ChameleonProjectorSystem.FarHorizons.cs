using Content.Shared.Polymorph.Components;

namespace Content.Client.Polymorph.Systems;

public sealed partial class ChameleonProjectorSystem
{
    [SubscribeLocalEvent]
    private void OnDisguiseStartup(Entity<ChameleonDisguiseComponent> ent, ref ComponentStartup args)
    {
        CopyVisuals(ent);
    }
}
