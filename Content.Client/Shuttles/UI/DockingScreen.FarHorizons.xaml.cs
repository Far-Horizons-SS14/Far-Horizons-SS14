using System.Linq;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.UI;

public sealed partial class DockingScreen
{
    const float MaxDockDistanceSq = SharedDockingSystem.DockRange * SharedDockingSystem.DockRange;

    private static LocId autoDockLabel = "shuttle-console-autodock";
    private static LocId autoUnDockLabel = "shuttle-console-autoundock";

    private bool _docked;
    private bool _dockable;

    private NetEntity? _shuttleNEnt;
    private List<DockingPortState> _docks = [];

    private void InitAutodock() => 
        FHAutoDock.OnPressed += AutoDockRequest;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (_shuttleNEnt == null)
            return;

        SetDocked();
        SetDockable();
        UpdateAutodockButton();
    }

    private void SetAutodockState(EntityUid? shuttle)
    {
        if (shuttle == null)
            return;
        
        _shuttleNEnt = _entManager.GetNetEntity(shuttle.Value);
        if (_shuttleNEnt == null || !Docks.TryGetValue(_shuttleNEnt.Value, out var shuttleDocks) || shuttleDocks.Count <= 0)
            return;
        _docks = shuttleDocks;
    }

    private void SetDocked() => 
        _docked = _docks.Any(dock => dock.Connected);

    private void SetDockable()
    {
        _dockable = false;

        if (_docked)
        {
            _dockable = true;
            return;
        }

        foreach (var dock in _docks)
        {
            var dockCoords = _entManager.GetCoordinates(dock.Coordinates);
            var dockMapCoords = _transform.ToMapCoordinates(dockCoords);
            var dockRot = _transform.GetWorldRotation(dockCoords.EntityId) + dock.Angle;

            foreach (var otherDock in Docks.Where(kv => kv.Key != _shuttleNEnt).SelectMany(kv => kv.Value))
            {
                var otherDockCoords = _entManager.GetCoordinates(otherDock.Coordinates);
                var otherDockMapCoords = _transform.ToMapCoordinates(otherDockCoords);

                var distanceSq = (dockMapCoords.Position - otherDockMapCoords.Position).LengthSquared();

                if (distanceSq > MaxDockDistanceSq)
                    continue;
                
                var otherDockRot = _transform.GetWorldRotation(otherDockCoords.EntityId) + otherDock.Angle;
                if (!_dock.InAlignment(dockMapCoords, dockRot, otherDockMapCoords, otherDockRot)) continue;
                
                _dockable = true;
                return;
            }
        }
    }

    private void UpdateAutodockButton()
    {
        FHAutoDock.Text = Loc.GetString(_docked ? autoUnDockLabel : autoDockLabel);
        FHAutoDock.Disabled = !_dockable;
    }

    private void AutoDockRequest(BaseButton.ButtonEventArgs args)
    {
        if (_shuttleNEnt == null ||
            !_dockable)
            return;

        if (_docked)
        {
            UndockAll();
            return;
        }

        DockAll();
    }

    private void UndockAll()
    {
        foreach (var dock in _docks.Where(dock => dock.Connected)) 
            UndockRequest?.Invoke(dock.Entity);
    }

    private void DockAll()
    {
        foreach (var dock in _docks)
        {
            var dockCoords = _entManager.GetCoordinates(dock.Coordinates);
            var dockMapCoords = _transform.ToMapCoordinates(dockCoords);
            var dockRot = _transform.GetWorldRotation(dockCoords.EntityId) + dock.Angle;

            foreach (var otherDock in Docks.Where(kv => kv.Key != _shuttleNEnt).SelectMany(kv => kv.Value))
            {
                var otherDockCoords = _entManager.GetCoordinates(otherDock.Coordinates);
                var otherDockMapCoords = _transform.ToMapCoordinates(otherDockCoords);

                var distanceSq = (dockMapCoords.Position - otherDockMapCoords.Position).LengthSquared();

                if (distanceSq > MaxDockDistanceSq)
                    continue;

                var otherDockRot = _transform.GetWorldRotation(otherDockCoords.EntityId) + otherDock.Angle;
                if (!_dock.InAlignment(dockMapCoords, dockRot, otherDockMapCoords, otherDockRot))
                    continue;

                DockRequest?.Invoke(dock.Entity, otherDock.Entity);
            }
        }
    }
}