using Content.Server.Administration;
using Content.Shared.Medical.Disease.Cures;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Commands;

/// <summary>
/// Grants cure from a disease to your attached entity.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class VaccinateCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedDiseaseCureSystem _cure = default!;
    public override string Command => "vaccinate";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 2)));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var net) || !EntityManager.TryGetEntity(net, out var resolved))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
            return;
        }

        var diseaseId = args[1];
        if (!_proto.HasIndex<DiseasePrototype>(diseaseId))
        {
            shell.WriteError(Loc.GetString("cmd-vaccinate-fail"));
            return;
        }

        var targetUid = resolved.Value;
        if (!EntityManager.TryGetComponent(targetUid, out DiseaseCarrierComponent? comp))
            comp = EntityManager.AddComponent<DiseaseCarrierComponent>(targetUid);

        if (_proto.TryIndex(diseaseId, out DiseasePrototype? disease))
            _cure.ApplyCureDisease((targetUid, comp), disease);

        shell.WriteLine(Loc.GetString("cmd-vaccinate-completed", ("target", targetUid.ToString()), ("disease", diseaseId)));
    }
}
