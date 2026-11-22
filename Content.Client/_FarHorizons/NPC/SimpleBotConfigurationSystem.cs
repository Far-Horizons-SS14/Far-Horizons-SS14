using Content.Client._FarHorizons.NPC.UI;
using Content.Shared._FarHorizons.NPC;
using Content.Shared.UserInterface;

namespace Content.Client._FarHorizons.NPC;

public sealed partial class SimpleBotConfigurationSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleBotConfigurationComponent, AfterAutoHandleStateEvent>(OnConfigurationState);
    }

    private void OnConfigurationState(Entity<SimpleBotConfigurationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<SimpleBotConfigurationBoundUserInterface>(
                ent.Owner,
                SimpleBotConfigurationUiKey.Key,
                out var bui))
        {
            bui.Refresh();
        }
    }
}
