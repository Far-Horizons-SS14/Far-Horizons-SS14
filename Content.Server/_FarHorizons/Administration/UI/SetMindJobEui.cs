using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._FarHorizons.Administration.SetMindJob;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server._FarHorizons.Administration.UI;

[UsedImplicitly]
public sealed class SetMindJobEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    private readonly NetUserId _target;

    public SetMindJobEui(NetUserId entity)
    {
        _target = entity;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();

        StateDirty();
        _adminManager.OnPermsChanged += AdminManagerOnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        return new SetMindJobEuiState
        {
            TargetNetUserId = _target,
        };
    }

    private void AdminManagerOnPermsChanged(AdminPermsChangedEventArgs obj)
    {
        // Close UI if user loses +FUN.
        if (obj.Player == Player && !UserAdminFlagCheck(AdminFlags.Fun))
        {
            Close();
        }
    }
    private bool UserAdminFlagCheck(AdminFlags flags)
    {
        return _adminManager.HasAdminFlag(Player, flags);
    }

}
