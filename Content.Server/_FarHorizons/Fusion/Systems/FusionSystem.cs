using Content.Server._FarHorizons.Fusion.Reactions;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem : EntitySystem
{
    public override void Initialize() {
        base.Initialize();

        CollectReactions();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<FusionReactionPrototype>())
                    CollectReactions();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessFusionDevices(); // TODO: not every update
    }
}