using System.Globalization;
using Content.Server.Administration;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Components;

namespace Content.Server._FarHorizons.Medical.Disease.Commands;

/// <summary>
/// Infects your attached entity with a disease at an optional stage.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class InfectCommand : LocalizedEntityCommands
{
    [Dependency] private SharedDiseaseSystem _disease = default!;
    [Dependency] private IPrototypeManager _proto = default!;

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

        var stage = 0;
        if (args.Length >= 3 && int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStage))
            stage = Math.Clamp(parsedStage, 0, 4);
        
        var disease = _disease.GenerateDisease(diseaseId);
        if(disease == null ) return;
        
        var stageData = _disease.CreateStage(disease.Value, stage);
        if(stageData == null) return;

        if (!_disease.Infect(targetUid, disease.Value, stageData))
        {
            shell.WriteError(Loc.GetString("cmd-infect-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-infect-completed", ("target", targetUid.ToString()), ("disease", diseaseId), ("stage", stage)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args) => args.Length switch
    {
        1 => CompletionResult.FromHintOptions(
            GetDiseaseCarrierOptions(),
            "<uid>"),
        2 => CompletionResult.FromHintOptions(
            CompletionHelper.PrototypeIDs<DiseasePrototype>(proto: _proto),
            "<disease prototype>"),
        3 => CompletionResult.FromHint("<stage>"),
        _ => CompletionResult.Empty,
    };

    private IEnumerable<CompletionOption> GetDiseaseCarrierOptions()
    {
        var query = EntityManager.EntityQueryEnumerator<DiseaseCarrierComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var meta))
        {
            var netEntity = EntityManager.GetNetEntity(uid);
            yield return new CompletionOption(netEntity.ToString(), string.IsNullOrEmpty(meta.EntityName) ? "unnamed" : meta.EntityName);
        }
    }
}
