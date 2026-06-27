using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.LimbDamage.UI;

[UsedImplicitly]
public sealed class LimbTargettingUIController : UIController, IOnSystemLoaded<LimbTargettingSystem>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private SpriteSystem? _sprite;
    private LimbTargettingSystem? _limbTargetting;

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
        _cfg.OnValueChanged(FHCCVars.LimbTargettingMatchSpecies, _ => UpdateWidget());
        _cfg.OnValueChanged(FHCCVars.LimbTargettingStyle, _ => UpdateWidget());
    }

    public void OnSystemLoaded(LimbTargettingSystem limbTargetting)
    {
        _limbTargetting = limbTargetting;
        _limbTargetting.LocalTargetUpdated += OnTargetUpdated;
    }

    private void OnTargetUpdated(ProtoId<OrganCategoryPrototype> target)
    {
        if (UIManager.ActiveScreen is null ||
            !UIManager.ActiveScreen.TryGetWidget<LimbTargettingUI>(out var widget))
            return;
        
        widget.UpdateLimb(target);
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
            !UIManager.ActiveScreen.TryGetWidget<LimbTargettingUI>(out var widget) ||
            _sprite is null ||
            _limbTargetting is null)
            return;

        if (_player.LocalSession?.AttachedEntity is not { } playerEnt ||
            !_entMan.TryGetComponent<LimbTargettingComponent>(playerEnt, out var limbTarget))
        {
            widget.ShutdownTarget();
            widget.Visible = false;
        }
        else
        {
            widget.InitTarget(_protoMan, _resourceCache, _sprite!, _limbTargetting!.GetTargetterStyle());
            widget.Visible = true;
            widget.UpdateLimb(limbTarget.Target);
        }

        if (widget.OnSelectedLimb == null)
            widget.OnSelectedLimb += SelectLimb;

    }

    private void SelectLimb(ProtoId<OrganCategoryPrototype> limb)
    {
        if (UIManager.ActiveScreen is null ||
            !UIManager.ActiveScreen.TryGetWidget<LimbTargettingUI>(out var widget) ||
            _limbTargetting is null)
            return;
        
        widget.UpdateLimb(limb);
        _limbTargetting.SetTarget(limb);
    }
}