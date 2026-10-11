using System.Linq;
using Content.Shared._FarHorizons.UI.BackgroundTraits;
using Content.Shared._Starlight.Traits.Effects;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.UI.BackgroundTraits;

public sealed partial class BackGroundTraitsSystem : SharedBackgroundTraitSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackgroundTraitsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BackgroundTraitsComponent, OpenBackgroundTraitsEvent>(OnOpenStore);
        SubscribeLocalEvent<BackgroundTraitsComponent, SubmitBackgroundTraitSelectionMessage>(OnSubmitTraits);
        
    }

    private void OnMapInit(Entity<BackgroundTraitsComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AllowTraitSelection)
            _actions.AddAction(ent, ent.Comp.TraitsAction);
    }

    private void OnSubmitTraits(Entity<BackgroundTraitsComponent> ent, ref SubmitBackgroundTraitSelectionMessage args)
    {
        if (args.Actor != ent.Owner ||
            !ent.Comp.AllowTraitSelection)
            return;

        var action = _actions.GetActions(ent)
            .Where(p => MetaData(p).EntityPrototype is { } entProto && entProto.ID == ent.Comp.TraitsAction)
            .FirstOrNull();

        if (action == null) return;

        _ui.CloseUi(ent.Owner, ent.Comp.TraitsUiKey);
        _actions.RemoveAction(ent.Owner, action!);

        var validated = ValidatedTraits(args.Selection);

        ent.Comp.AllowTraitSelection = false;
        ent.Comp.SelectedTraits = validated;

        var effectCtx = new TraitEffectContext
        {
            Player = ent,
            EntMan = EntityManager,
            Proto = ProtoMan,
            CompFactory = _compFactory,
            LogMan = _log,
            Transform = Transform(ent),
        };

        foreach (var effect in validated.Select(tid => ProtoMan.Index(tid)).SelectMany(trait => trait.Effects))
            effect.Apply(effectCtx);
    }

    private void OnOpenStore(Entity<BackgroundTraitsComponent> ent, ref OpenBackgroundTraitsEvent args)
    {
        if (_ui.IsUiOpen(ent.Owner, ent.Comp.TraitsUiKey)) return;
        
        _ui.OpenUi(ent.Owner, ent.Comp.TraitsUiKey, ent.Owner);
    }
}