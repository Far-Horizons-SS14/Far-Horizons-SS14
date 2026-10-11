using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.PowerArmor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.PowerArmor.UI;

[UsedImplicitly]
public sealed partial class PowerArmorStatusDisplayUIController : UIController
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private SpriteSystem? _sprite;

    public override void Initialize()
    {
        base.Initialize();
        
        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;

        _player.LocalPlayerAttached += OnCharacterAttached;
        _player.LocalPlayerDetached += OnCharacterDetached;
        _entMan.EntityDirtied += OnDirty;
        _entMan.ComponentRemoved += OnComponentRemoved;
        _protoMan.PrototypesReloaded += OnProtoReload;
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<LimbTargettingPrototype>()) return;

        UpdateWidget();
    }

    private void OnComponentRemoved(RemovedComponentEventArgs e)
    {
        if (_player.LocalSession?.AttachedEntity is not { } playerEnt ||
            playerEnt != e.BaseArgs.Owner ||
            e.Terminating)
            return;
        
        UpdateWidget();
    }

    private void OnDirty(Entity<MetaDataComponent> ent)
    {
        if (_player.LocalSession?.AttachedEntity is not { } playerEnt ||
            playerEnt != ent.Owner)
            return;
        
        UpdateWidget();
    }

    private void OnScreenLoad()
    {
        if (UIManager.ActiveScreen == null)
            return;

        UpdateWidget();
    }

    private void OnCharacterAttached(EntityUid uid) =>
        UpdateWidget();

    private void OnCharacterDetached(EntityUid obj) => 
        UpdateWidget();

    private void UpdateWidget()
    {
        _sprite ??= _entMan.SystemOrNull<SpriteSystem>();

        if (UIManager.ActiveScreen is null ||
            !UIManager.ActiveScreen.TryGetWidget<PowerArmorStatusDisplayUI>(out var widget) ||
            _sprite is null)
            return;

        if (_player.LocalSession?.AttachedEntity is not { } playerEnt ||
            !_entMan.HasComponent<PowerArmorUserComponent>(playerEnt))
        {
            widget.ShutdownTarget();
            widget.Visible = false;
        }
        else
        {
            widget.InitTarget(_protoMan, _resourceCache, _sprite!, "LimbTargetPowerArmor");
            widget.Visible = true;
        }
    }
}