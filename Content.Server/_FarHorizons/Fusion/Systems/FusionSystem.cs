using Content.Server._FarHorizons.Fusion.Reactions;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();
        CollectReactions();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<FusionReactionPrototype>()
            || args.WasModified<FusionDecayPrototype>()
            || args.WasModified<FusionConversionPrototype>())
        {
            CollectReactions();
        }
    }
}