using Content.Server.Shuttles.Components;
using Content.Server._FarHorizons.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;
public sealed partial class ShuttleSystem
{
    [SubscribeLocalEvent]
    private void OnRandomGridFillMapInit(Entity<RandomGridFillComponent> ent, ref MapInitEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        if (!TryComp<DockingComponent>(ent, out var dock) ||
            !TryComp(ent, out TransformComponent? xform) ||
            xform.GridUid == null)
        {
            return;
        }

        if (ent.Comp.PathWeights.Count == 0) {
            Log.Error($"Error loading gridfill dock {ToPrettyString(ent)} due to lacking any PathWeights");
            return;
        }

        var untriedGrids = new Dictionary<ResPath, float>(ent.Comp.PathWeights);

        while (_random.TryPickAndTake(untriedGrids, out var selectedGridPath)) {

            // Spawn on a dummy map and try to dock if possible, otherwise dump it.
            _mapSystem.CreateMap(out var tempMapId);
            var valid = false;

            if (_loader.TryLoadGrid(tempMapId, selectedGridPath, out var grid))
            {
                var escape = GetSingleDock(grid.Value);

                if (escape != null)
                {
                    var config = _dockSystem.GetDockingConfig(grid.Value, xform.GridUid.Value, escape.Value.Entity, escape.Value.Component, ent, dock);

                    if (config != null)
                    {
                        var shuttleXform = Transform(grid.Value);
                        FTLDock((grid.Value, shuttleXform), config);

                        if (TryComp<StationMemberComponent>(xform.GridUid, out var stationMember))
                        {
                            _station.AddGridToStation(stationMember.Station, grid.Value);
                        }

                        valid = true;
                    }
                }

                foreach (var compReg in ent.Comp.AddComponents.Values)
                {
                    var compType = compReg.Component.GetType();

                    if (HasComp(grid.Value, compType))
                        continue;

                    var comp = Factory.GetComponent(compType);
                    AddComp(grid.Value, comp, true);
                }
            }
            else
            {
                Log.Info($"Failed to place {selectedGridPath} for gridfill of dock {ToPrettyString(ent)}, cycling");
            }

            _mapSystem.DeleteMap(tempMapId);

            if (valid)
            {
                return;
            }
        }

        Log.Error($"Error placing all possible gridfills for gridfill dock {ToPrettyString(ent)}");
        DebugTools.Assert($"Error placing all possible gridfills for gridfill dock {ToPrettyString(ent)}");
    }
}
