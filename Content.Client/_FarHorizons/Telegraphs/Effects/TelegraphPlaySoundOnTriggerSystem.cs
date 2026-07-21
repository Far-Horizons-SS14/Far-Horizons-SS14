using System.Numerics;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphPlaySoundOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphPlaySoundOnTriggerComponent, OnTelegraphTriggered>(OnTriggered);
    }

    private void OnTriggered(Entity<TelegraphPlaySoundOnTriggerComponent> ent, ref OnTelegraphTriggered args)
    {
        if (!_timing.IsFirstTimePredicted) return;
        if (!_transform.TryGetMapOrGridCoordinates(ent, out var coords)) return;
        _audio.PlayStatic(_audio.ResolveSound(ent.Comp.Sound), Filter.Pvs(coords.Value, 10, EntityManager, _playerMan), coords.Value, true);
    }
}