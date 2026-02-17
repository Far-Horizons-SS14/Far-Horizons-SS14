using Robust.Shared.Containers;

namespace Content.Server._FarHorizons.AutoImplanter;

[RegisterComponent]
public sealed partial class AutoImplanterComponent : Component
{
    public const string ContainerId = "autoImplant";

    [ViewVariables] public Container Container = default!;
}