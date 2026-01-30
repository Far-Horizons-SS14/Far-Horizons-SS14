using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Administration.SetMindJob;


[Serializable, NetSerializable]
public sealed class SetMindJobEuiState : EuiStateBase
{
    public NetUserId TargetNetUserId;
}
