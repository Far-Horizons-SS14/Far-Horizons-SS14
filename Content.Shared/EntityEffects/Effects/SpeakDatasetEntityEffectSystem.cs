using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Network;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes the entity to speak a random localized line from the given dataset immediately.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpeakDatasetEntityEffectSystem : EntityEffectSystem<MetaDataComponent, SpeakDataset>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SpeakDataset> args)
    {
        // TODO: When we get proper random prediction remove this check.
        if (_net.IsClient)
            return;

        if (!_prototype.TryIndex(args.Effect.PackId, out var pack))
            return;

        var message = Loc.GetString(_random.Pick(pack));
        _chat.TrySendInGameICMessage(entity.Owner, message, InGameICChatType.Speak, args.Effect.HideInChat);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpeakDataset : EntityEffectBase<SpeakDataset>
{
    /// <summary>
    /// Dataset of localized lines to speak.
    /// </summary>
    [DataField("pack", required: true)]
    public ProtoId<LocalizedDatasetPrototype> PackId;

    /// <summary>
    /// If true, suppress chat window output.
    /// </summary>
    [DataField]
    public bool HideInChat = false;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-speak-dataset", ("chance", Probability)); // TODO: localized dataset names are needed.
}
