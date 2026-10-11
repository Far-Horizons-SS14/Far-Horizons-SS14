using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Vampire;

public abstract partial class SharedLesserVampireSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StomachSystem _stomach = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private ReactiveSystem _reaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private ThirstSystem _thirst = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeVerbs();
        InitializeDrinking();

        SubscribeLocalEvent<LesserVampireComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<LesserVampireComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !IsFangsVisible(ent) ||
            !_ingestion.HasMouthAvailable(ent, ent))
            return;

        args.PushMarkup(Loc.GetString("lesser-vampire-fangs-examine", ("vampire", Identity.Entity(ent, EntityManager))));
    }

    public bool IsFangsVisible(Entity<LesserVampireComponent> ent)
    {
        var ev = new VampireFangsCheck();
        RaiseLocalEvent(ent, ref ev);

        return !ev.FangsHidden;
    }
}