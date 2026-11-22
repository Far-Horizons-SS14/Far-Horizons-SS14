using Content.Server.Actions;
using Content.Server.Administration.Systems;
using Content.Server.Hands.Systems;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private StarlightEntitySystem _slEnt = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!; //Far Horizons

    public override void Initialize()
    {
        base.Initialize();
        InitializeLimbWithItems();
    }
}
