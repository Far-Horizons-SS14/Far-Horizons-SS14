using Content.Shared._FarHorizons.Vampire;
using Content.Shared.Alert;
using Robust.Client.Player;

namespace Content.Client._FarHorizons.Vampire;

public sealed class LesserVampireSystem : SharedLesserVampireSystem
{
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private static readonly TimeSpan _updateRate = TimeSpan.FromSeconds(1f);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_playerMan.LocalEntity is not { } localPlayer)
            return;
        
        if (Timing.CurTime < _nextUpdate) return;
        _nextUpdate = Timing.CurTime + _updateRate;

        if (!TryComp<LesserVampireComponent>(localPlayer, out var vamp))
            return;

        ShowAlerts((localPlayer, vamp));
    }

    private void ShowAlerts(Entity<LesserVampireComponent> ent)
    {
        var currBloodPool = GetBloodPool(ent);
        var curLevel = (short)(currBloodPool / ent.Comp.BloodPoolPerLevel);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BloodPoolAlert, curLevel);
    }
}