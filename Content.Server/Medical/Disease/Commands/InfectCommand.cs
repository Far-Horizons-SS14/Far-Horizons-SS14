using System.Globalization;
using Content.Server.Administration;
using Content.Shared.Medical.Disease.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Medical.Disease.Commands;

/// <summary>
/// Infects your attached entity with a disease at an optional stage.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class InfectCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    public override string Command => "infect";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 2)));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var parsedNet) || !EntityManager.TryGetEntity(parsedNet, out var parsedUid))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        var targetUid = parsedUid.Value;
        var diseaseId = args[1];

        var stage = 1;
        if (args.Length >= 3 && int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStage))
            stage = Math.Max(1, parsedStage);

        if (!_disease.Infect(targetUid, diseaseId, stage))
        {
            shell.WriteError(Loc.GetString("cmd-infect-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-infect-completed", ("target", targetUid.ToString()), ("disease", diseaseId), ("stage", stage)));
    }
}
