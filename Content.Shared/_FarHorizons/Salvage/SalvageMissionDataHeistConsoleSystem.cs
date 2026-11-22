using Content.Shared._FarHorizons.Salvage.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._FarHorizons.Salvage;

public sealed partial class SalvageMissionDataHeistConsoleSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMissionDataHeistConsoleComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<SalvageMissionDataHeistConsoleComponent, SalvageMissionDisarmSubmitCodeMessage>(OnSubmitCode);
    }

    private void OnUiOpenAttempt(Entity<SalvageMissionDataHeistConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!ent.Comp.Enabled || !ent.Comp.HasData)
            args.Cancel();
    }

    private void OnSubmitCode(Entity<SalvageMissionDataHeistConsoleComponent> ent, ref SalvageMissionDisarmSubmitCodeMessage args)
    {
        if (_transform.GetGrid(ent.Owner) is not {} grid ||
            !TryComp<SalvageMissionDataHeistPasswordBankComponent>(grid, out var passwordBank) ||
            !passwordBank.Codes.Contains(args.Code) ||
            !ent.Comp.HasData ||
            !TryComp<SalvageMissionObjectiveTargetComponent>(ent.Owner, out var missionTarget))
        {
            _audio.PlayPvs(ent.Comp.FailSound, ent.Owner);
            return;
        }

        _audio.PlayPvs(ent.Comp.SuccessSound, ent.Owner);
        var spawned = SpawnAtPosition(ent.Comp.SpawnTarget, Transform(ent.Owner).Coordinates);
        var targetComp = EnsureComp<SalvageMissionObjectiveTargetComponent>(spawned);
        targetComp.OwnedBy = missionTarget.OwnedBy;
        passwordBank.Codes.Remove(args.Code);
        RemComp<SalvageMissionObjectiveTargetComponent>(ent);
        ent.Comp.HasData = false;
        _appearance.SetData(ent, SalvageMissionDisarmConsoleVisuals.Armed, true);
        _ui.CloseUis(ent.Owner);
    }

    public void SetupConsole(Entity<SalvageMissionDataHeistConsoleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        ent.Comp.Enabled = true;
        ent.Comp.HasData = true;

        _appearance.SetData(ent, SalvageMissionDisarmConsoleVisuals.Enabled, true, appearance);
        _appearance.SetData(ent, SalvageMissionDisarmConsoleVisuals.Armed, false, appearance);
    }
}