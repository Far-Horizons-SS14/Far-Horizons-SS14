using System.Linq;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.LimbDamage.UI;

public sealed class LimbTargettingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public Action<ProtoId<OrganCategoryPrototype>>? LocalTargetUpdated;

    private const string DefaultStyle = "LimbTargetHuman";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbTargettingComponent, AfterAutoHandleStateEvent>(OnState);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TargetNextLimb, new PointerInputCmdHandler(CycleForward, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetPreviousLimb, new PointerInputCmdHandler(CycleBackward, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetTorso, new PointerInputCmdHandler(SelectTorso, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetHead, new PointerInputCmdHandler(SelectHead, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetArmLeft, new PointerInputCmdHandler(SelectArmLeft, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetArmRight, new PointerInputCmdHandler(SelectArmRight, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetHandLeft, new PointerInputCmdHandler(SelectHandLeft, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetHandRight, new PointerInputCmdHandler(SelectHandRight, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetLegLeft, new PointerInputCmdHandler(SelectLegLeft, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetLegRight, new PointerInputCmdHandler(SelectLegRight, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetFootLeft, new PointerInputCmdHandler(SelectFootLeft, outsidePrediction: true))
            .Bind(ContentKeyFunctions.TargetFootRight, new PointerInputCmdHandler(SelectFootRight, outsidePrediction: true))
            .Register<LimbTargettingUI>();
    }

    private void OnState(Entity<LimbTargettingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent != _player.LocalEntity) return;
        LocalTargetUpdated?.Invoke(ent.Comp.Target);
    }

    public void SetTarget(ProtoId<OrganCategoryPrototype> target) => 
        RaiseNetworkEvent(new ChangeLimbTargetMessage(target));
    
    public ProtoId<LimbTargettingPrototype> GetTargetterStyle()
    {
        var style = _cfg.GetCVar(FHCCVars.LimbTargettingStyle);

        if (_cfg.GetCVar(FHCCVars.LimbTargettingMatchSpecies) &&
            _player.LocalSession?.AttachedEntity is { } playerEnt &&
            TryComp<LimbDamageableComponent>(playerEnt, out var limbDamageable))
            style = limbDamageable.Proto;

        if (!_protoMan.HasIndex<LimbTargettingPrototype>(style))
            style = DefaultStyle;

        return style;
    }

    private ProtoId<OrganCategoryPrototype>? CycleTarget(bool forward = true)
    {
        if (_player.LocalEntity == null ||
            !TryComp<LimbTargettingComponent>(_player.LocalEntity, out var limbTargetting))
            return null;

        var targets = _protoMan.Index(GetTargetterStyle()).Limbs.Select(p => p.Limb).ToList();
        var curIndex = targets.IndexOf(limbTargetting.Target);
        int newIndex;
        if (forward)
            newIndex = curIndex + 1 >= targets.Count ? 0 : curIndex + 1;
        else
            newIndex = curIndex - 1 < 0 ? targets.Count - 1 : curIndex - 1;

        return targets[newIndex];
    }

    private bool HandleInput(ProtoId<OrganCategoryPrototype> target)
    {
        if (_player.LocalEntity == null ||
            !TryComp<LimbTargettingComponent>(_player.LocalEntity, out var limbTargetting))
            return false;

        limbTargetting.Target = target;
        LocalTargetUpdated?.Invoke(target);
        RaiseNetworkEvent(new ChangeLimbTargetMessage(target));
        return true;
    }

    private bool CycleForward(in PointerInputCmdHandler.PointerInputCmdArgs args) => CycleTarget() is {} newTarget && HandleInput(newTarget);
    private bool CycleBackward(in PointerInputCmdHandler.PointerInputCmdArgs args) => CycleTarget(false) is {} newTarget && HandleInput(newTarget);

    private bool SelectTorso(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("Torso");
    private bool SelectHead(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("Head");
    private bool SelectArmLeft(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("ArmLeft");
    private bool SelectArmRight(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("ArmRight");
    private bool SelectHandLeft(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("HandLeft");
    private bool SelectHandRight(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("HandRight");
    private bool SelectLegLeft(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("LegLeft");
    private bool SelectLegRight(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("LegRight");
    private bool SelectFootLeft(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("FootLeft");
    private bool SelectFootRight(in PointerInputCmdHandler.PointerInputCmdArgs args) => HandleInput("FootRight");
}