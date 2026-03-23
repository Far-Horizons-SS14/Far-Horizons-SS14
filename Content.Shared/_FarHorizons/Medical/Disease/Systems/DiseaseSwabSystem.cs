using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Medical.Disease.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease.Systems;

/// <summary>
/// Handles using a disease sample swab on mobs to collect their active diseases.
/// </summary>
public sealed class DiseaseSwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseSampleComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DiseaseSampleComponent, DiseaseSwabDoAfterEvent>(OnDoAfter);
    }

    private const float SwabDelaySeconds = 2f;

    /// <summary>
    /// Starts a timed swab action on a living mob when the swab is used.
    /// </summary>
    private void OnAfterInteract(Entity<DiseaseSampleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // Only allow swabbing living mobs
        if (_mobState.IsDead(target))
            return;

        // Don't allow swabbing machines like diagnoser here
        if (HasComp<DiseaseDiagnoserComponent>(target))
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, SwabDelaySeconds, new DiseaseSwabDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// On do-after completion: records the target's active diseases and basic identity info into the swab.
    /// </summary>
    private void OnDoAfter(Entity<DiseaseSampleComponent> ent, ref DiseaseSwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        // Read diseases from carrier and overwrite sample
        ent.Comp.Diseases.Clear();
        ent.Comp.Stages.Clear();
        ent.Comp.HasSample = true;
        ent.Comp.SubjectName = Identity.Name(target, EntityManager);
        ent.Comp.SubjectDNA = null;

        if (TryComp<DnaComponent>(target, out var dna) && !string.IsNullOrWhiteSpace(dna.DNA))
            ent.Comp.SubjectDNA = dna.DNA;

        if (TryComp<DiseaseCarrierComponent>(target, out var carrier) && carrier.ActiveDiseases.Count > 0)
        {
            foreach (var (diseaseId, stage) in carrier.ActiveDiseases)
            {
                if (!_prototypes.HasIndex(diseaseId))
                    continue;

                ent.Comp.Diseases.Add(diseaseId);
                ent.Comp.Stages[diseaseId] = stage;
            }

            _popup.PopupPredicted(Loc.GetString("swab-disease-collected-popup"), target, args.Args.User);
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString("swab-disease-no-diseases-popup"), target, args.Args.User);
        }

        Dirty(ent);

        args.Handled = true;
    }
}
