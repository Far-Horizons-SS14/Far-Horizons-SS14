using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server._FarHorizons.Vehicles.Atmos;

/// <summary>
/// Handles atmospheric systems for mechs including air circulation, fans, and life support.
/// </summary>
public sealed class VehicleAtmosphereSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    private const float MinExternalPressure = 0.05f;
    private const float PressureTolerance = 0.1f;

    /// <inheritdoc/>
    public override void Initialize()
    {        
        SubscribeLocalEvent<VehicleEquipmentComponent, VehicleFanToggle>(OnFanToggle);

        SubscribeLocalEvent<RiderComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<RiderComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<RiderComponent, AtmosExposedGetAirEvent>(OnExpose);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehicleModsComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateFanModule((uid, component), frameTime);
            UpdateCabinPressure((uid, component));
        }
    }

    #region Cabin

    public bool TryGetGasModuleAir(Entity<VehicleModsComponent> ent, out GasMixture? air)
    {
        air = null;
        if(ent.Comp.Equipment[EquipmentType.AIRTANK] == null) 
            return false;
        if (!TryComp<GasTankComponent>(ent.Comp.Equipment[EquipmentType.AIRTANK], out var tank))
            return false;

        air = tank.Air;
        return true;
    }

    private bool UpdateCabinPressure(Entity<VehicleModsComponent> ent)
    {
        if (!TryComp<VehicleCabinAirComponent>(ent.Owner, out var cabin))
            return false;

        if (!TryGetGasModuleAir(ent, out var tankAir) || tankAir == null)
            return false;

        return _atmosphere.PumpGasTo(tankAir, cabin.Air, cabin.TargetPressure);
    }

    private void OnInhale(Entity<RiderComponent> ent, ref InhaleLocationEvent args)
    {
        if(ent.Comp.Riding == null) return;
        if (!TryComp<VehicleComponent>(ent.Comp.Riding, out var vehicleComp))
            return;
        // Meter a single breath from the cabin using a tank-like regulator pressure.
        if (TryComp<VehicleCabinAirComponent>(ent.Comp.Riding, out var cabin))
        {
            var vol = args.Respirator.BreathVolume;
            var breath = new GasMixture(vol)
            {
                Temperature = cabin.Air.Temperature
            };
            var pressure = cabin.RegulatorPressure;
            _atmosphere.PumpGasTo(cabin.Air, breath, pressure);
            args.Gas = breath;
            return;
        }
        args.Gas = _atmosphere.GetContainingMixture(ent.Comp.Riding.Value, excite: true);
    }

    private void OnExhale(Entity<RiderComponent> ent, ref ExhaleLocationEvent args)
    {
        if(ent.Comp.Riding == null) return;
        if (!TryComp<VehicleComponent>(ent.Comp.Riding, out var vehicleComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Riding.Value, vehicleComp));
    }

    private void OnExpose(Entity<RiderComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if(ent.Comp.Riding == null) return;
        if (!TryComp<VehicleComponent>(ent.Comp.Riding, out var vehicleComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Riding.Value, vehicleComp), args.Excite);
        args.Handled = true;
    }

    private GasMixture? GetBreathMixture(Entity<VehicleComponent> ent, bool excite = true)
    {
        if (TryComp<VehicleCabinAirComponent>(ent.Owner, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(ent.Owner, excite: excite);
    }

    #endregion

    #region Fan

    private bool UpdateFanModule(Entity<VehicleModsComponent> ent, float frameTime)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null || !fanModule.IsActive)
        {
            if (fanModule != null)
                SetFanState(ent, fanModule, FanState.Off);

            return false;
        }

        var (tankComp, tankAir) = GetGasTank(ent);
        if (tankAir == null || tankComp == null)
        {
            SetFanState(ent, fanModule, FanState.Off);
            return false;
        }

        if (ent.Comp.Equipment[EquipmentType.WASTETANK] == null 
            || !TryComp<GasTankComponent>(ent.Comp.Equipment[EquipmentType.WASTETANK], out var wasteComp))
        {
            SetFanState(ent, fanModule, FanState.Off);
            return false;
        }

        return ProcessFanOperation(ent, fanModule, tankComp, tankAir, wasteComp.Air, frameTime);
    }

    private (GasTankComponent? tank, GasMixture? air) GetGasTank(Entity<VehicleModsComponent> ent)
    {
        if (TryComp<GasTankComponent>(ent.Comp.Equipment[EquipmentType.AIRTANK], out var tank))
                return (tank, tank.Air);

        return (null, null);
    }

    private bool ProcessFanOperation(Entity<VehicleModsComponent> ent,
        VehicleFanModComponent fanModule,
        GasTankComponent tankComp,
        GasMixture tankAir,
        GasMixture wasteAir,
        float frameTime)
    {        
        var external = _atmosphere.GetContainingMixture(ent.Owner);
        
        if(TryComp<VehicleCabinAirComponent>(ent.Owner, out var vcaComp))
            _atmosphere.ScrubInto(vcaComp.Air, wasteAir, fanModule.FilterGases);

        if (external == null
            || external.Pressure <= MinExternalPressure
            || tankAir.Pressure >= tankComp.MaxOutputPressure - PressureTolerance)
        {
            SetFanState(ent, fanModule, FanState.Idle);
            return false;
        }

        var success = ProcessFilteredTransfer(external, tankAir, wasteAir, fanModule, frameTime);
        SetFanState(ent, fanModule, success ? FanState.On : FanState.Idle);
        return success;
    }

    private bool ProcessFilteredTransfer(GasMixture external,
        GasMixture tankAir,
        GasMixture wasteAir,
        VehicleFanModComponent fanModule,
        float frameTime)
    {
        // Calculate transfer volume based on processing rate.
        var transferVolume = fanModule.GasProcessingRate * frameTime;
        if (transferVolume <= 0)
            return false;

        var wasteRemoved = wasteAir.RemoveVolume(transferVolume);
        if(wasteRemoved.TotalMoles >= 0)
            _atmosphere.Merge(external, wasteRemoved);

        // Remove gas from external environment.
        var removed = external.RemoveVolume(transferVolume);
        if (removed.TotalMoles <= 0)
            return false;

        if (fanModule is { FilterGases.Count: > 0 })
        {
            var filtered = new GasMixture { Temperature = removed.Temperature };
            _atmosphere.ScrubInto(removed, filtered, fanModule.FilterGases);
            // Return filtered gases to external environment.
            _atmosphere.Merge(external, filtered);
        }
        

        // Add remaining gas to internal tank (either unfiltered, or post-scrub remainder).
        _atmosphere.Merge(tankAir, removed);
        return true;
    }

    public void SetFanState(Entity<VehicleModsComponent> ent, VehicleFanModComponent fanModule, FanState state)
    {
        if (fanModule.State == state)
            return;

        fanModule.State = state;
        Dirty(ent);
    }

    private void OnFanToggle(Entity<VehicleEquipmentComponent> ent, ref VehicleFanToggle args)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null || args.Handled)
            return;
        fanModule.IsActive = !fanModule.IsActive;

        // Set the correct state based on the toggle.
        var newState = fanModule.IsActive ? FanState.On : FanState.Off;
        if (fanModule.State != newState)
        {
            fanModule.State = newState;
            Dirty(ent);
        }
        args.Handled = true;
    }
    
    private VehicleFanModComponent? GetFanModule(EntityUid ent)
    {
        // Direct component case
        if (TryComp<VehicleFanModComponent>(ent, out var fanModule))
            return fanModule;

        // Equipment case
        if (!TryComp<VehicleModsComponent>(ent, out var vmComp))
            return null;

        if (!vmComp.Equipment.TryGetValue(EquipmentType.VENTFAN, out var fanUid))
            return null;

        if (!TryComp(fanUid, out fanModule))
            return null;

        return fanModule;
    }

    #endregion
}