using Content.Server.FarHorizons.Tools.Shipyard.Components;
using Content.Server.PowerCell;
using Content.Shared.FarHorizons.Tools.Shipyard;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.FarHorizons.Tools.Shipyard.Systems;

public sealed class IntegrityAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntegrityAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<IntegrityAnalyzerComponent, IntegrityAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<IntegrityAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<IntegrityAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<IntegrityAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<IntegrityAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not {} target)
                continue;

            if (Deleted(target))
            {
                StopAnalyzingEntity((uid, component), target);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between integrity analyzer and the scanned entity
            //null is infinite range
            var targetCoordinates = Transform(target).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(targetCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                //Range too far, disable updates
                StopAnalyzingEntity((uid, component), target);
                continue;
            }

            UpdateScannedTarget(uid, target, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<IntegrityAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null 
            || !TryComp<DamageableComponent>(args.Target, out var damageableComponent)
            || HasComp<MobStateComponent>(args.Target)
            || !_cell.HasDrawCharge(uid, user: args.User))
            return;
        
        if (uid.Comp.DamageContainers != null 
            && damageableComponent.DamageContainerID != null 
            && !uid.Comp.DamageContainers.Contains(damageableComponent.DamageContainerID))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new IntegrityAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = false,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;
    }

    private void OnDoAfter(Entity<IntegrityAnalyzerComponent> uid, ref IntegrityAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<IntegrityAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } target)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<IntegrityAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } target)
            StopAnalyzingEntity(ent, target);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<IntegrityAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } target)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, IntegrityAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, IntegrityAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its integrity analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="integrityAnalyzer">The integrity analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    public void BeginAnalyzingEntity(Entity<IntegrityAnalyzerComponent> integrityAnalyzer, EntityUid target)
    {
        //Link the integrity analyzer to the scanned entity
        integrityAnalyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(integrityAnalyzer.Owner);

        UpdateScannedTarget(integrityAnalyzer, target, true);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="integrityAnalyzer">The integrity analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    public void StopAnalyzingEntity(Entity<IntegrityAnalyzerComponent> integrityAnalyzer, EntityUid target)
    {
        //Unlink the analyzer
        integrityAnalyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(integrityAnalyzer.Owner);

        UpdateScannedTarget(integrityAnalyzer, target, false);
    }

    /// <summary>
    /// Send an update for the target to the integrityAnalyzer
    /// </summary>
    /// <param name="integrityAnalyzer">The integrity analyzer</param>
    /// <param name="target">The entity being scanned</param>
    public void UpdateScannedTarget(EntityUid integrityAnalyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(integrityAnalyzer, IntegrityAnalyzerUiKey.Key) || !TryComp<IntegrityAnalyzerComponent>(integrityAnalyzer, out var integrityAnalyzerComp))
            return;

        if (!HasComp<DamageableComponent>(target))
            return;

        _uiSystem.ServerSendUiMessage(integrityAnalyzer, IntegrityAnalyzerUiKey.Key, new IntegrityAnalyzerScannedTargetMessage(
            GetNetEntity(target),
            scanMode
        ));
    }
}
