using Content.Client.CartridgeLoader.Cartridges;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.CartridgeLoader.Cartridges;

public sealed partial class OperationsUi : UIFragment
{
    private OperationsUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new OperationsUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case CCOperationsUIState cast:
                _fragment?.UpdateState(cast.Agents);
                break;
        }
    }
}