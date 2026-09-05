using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body;
using Content.Shared.Cargo;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

[UsedImplicitly]
public sealed partial class GasTankSystem : SharedGasTankSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedAudioSystem _audioSys = default!;

    private const float MinimumSoundValvePressure = 10.0f;

    private const float ReleaseArea = 0.0001f; // About 1cm^2
    private const float SafeReleaseArea = 0.01f; // Far Horizons - split maxcap and non-maxcap release logic 

    // A vector bias for throwing our gas tanks in radians. Averages about -43 degrees since the sprite is at a 45-degree angle.
    private static readonly Vector2 ThrowVector = new (-1.0f, -0.5f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTankComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<GasTankComponent, GasAnalyzerScanEvent>(OnAnalyzed);
        SubscribeLocalEvent<GasTankComponent, PriceCalculationEvent>(OnGasTankPrice);
    }

    protected override void DeviceUpdated(Entity<GasTankComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        // Release gas if valve is open
        // Disconnect from internals if valve is open
        if (entity.Comp.ReleaseValveOpen)
        {
            DisconnectFromInternals(entity);
            ReleaseGas(entity, args.dt);
        }
        else if (entity.Comp.CheckUser)
        {
            entity.Comp.CheckUser = false;
            if (Transform(entity).ParentUid != entity.Comp.User)
            {
                DisconnectFromInternals(entity);
            }
        }

        Atmos.React(entity.Comp.Air, entity.Comp);

        //Far Horizons Start
        if ((entity.Comp.IsConnected || entity.Comp.ReleaseValveOpen) &&
            (UI.IsUiOpen(entity.Owner, SharedGasTankUiKey.Key) || UI.IsUiOpen(entity.Owner, SharedGasTankUiKey.OrganKey)))
            UpdateUserInterface(entity);
        //Far Horizons End
    }

    public override void UpdateUserInterface(Entity<GasTankComponent> ent)
    {
        var (owner, component) = ent;
        var state = new GasTankBoundUserInterfaceState
        {
            TankPressure = component.Air.Pressure
        };
        UI.SetUiState(owner, SharedGasTankUiKey.Key, state);
        UI.SetUiState(owner, SharedGasTankUiKey.OrganKey, state);
    }

    /// <summary>
    /// logic to empty the Gas Tank Organ
    /// </summary>
    protected override void OnGasTankEmptyOrgan(Entity<GasTankComponent> ent, ref GasTankEmptyOrganMessage args)
    {
        // Skip if the organ is already empty, to avoid spam
        if (ent.Comp.Air == null || ent.Comp.Air.TotalMoles <= 0)
            return;

         // Get the gas
        var environment = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);
        if (environment != null)
        {
            // Send the gas to the environment
            _atmosphereSystem.Merge(environment, ent.Comp.Air);
        }

        // Clear the gas tank
        ent.Comp.Air.Clear();
        CheckStatus(ent);

        // Play sound on release
         EntityUid soundSource = ent.Owner;
        if (TryComp<OrganComponent>(ent.Owner, out var organ) && organ.Body != null)
        {
            soundSource = organ.Body.Value;
        }
        _audioSys.PlayPvs(ent.Comp.RuptureSound, soundSource);

        Dirty(ent);
        UpdateUserInterface(ent);
        // Starlight edit end
    }

    private void OnParentChange(EntityUid uid, GasTankComponent component, ref EntParentChangedMessage args)
    {
        // When an item is moved from hands -> pockets, the container removal briefly dumps the item on the floor.
        // So this is a shitty fix, where the parent check is just delayed. But this really needs to get fixed
        // properly at some point.
        component.CheckUser = true;
    }

    /// <summary>
    /// Tries to release gas through the pressure release valve.
    /// </summary>
    /// <param name="entity">The gas tank entity releasing gas</param>
    /// <param name="dt">The amount of time since the last update</param>
    /// <returns></returns>
    private void ReleaseGas(Entity<GasTankComponent> entity, float dt)
    {
        var environment = _atmosphereSystem.GetContainingMixture(entity.Owner, false, true);

        var deltaP = environment == null
            ? entity.Comp.Air.Pressure
            : entity.Comp.Air.Pressure - environment.Pressure;

        // Far Horizons start
        // Removed cap. Despite it looking like an intentional mechanic, it keeps being reported as bug

        // Cap deltaP by the maximum output pressure of the tank.
        // if (deltaP < entity.Comp.SafetyPressure)
        //     deltaP = Math.Min(entity.Comp.ReleasePressure, deltaP);

        // Note, this might be a bad change, I have no idea how atmos works
        // However, it seems like release area is the most influential factor for how fast gas leaves the tank
        // If I fuck around with base one - maxcaps become nukes that phase through walls, so I'm keeping maxcap logic the same while changing normal gas release
        var removed = deltaP > entity.Comp.SafetyPressure
            ? _atmosphereSystem.FlowGas(entity.Comp.Air, deltaP, dt, ReleaseArea)
            : _atmosphereSystem.FlowGas(entity.Comp.Air, deltaP, dt, SafeReleaseArea);
        // Far Horizons end

        if (removed == null)
            return;

        if (environment != null)
            _atmosphereSystem.Merge(environment, removed);

        // If we wouldn't produce a sound, don't throw or play a sound.
        if (deltaP < MinimumSoundValvePressure)
            return;

        Audio.PlayPvs(entity.Comp.ReleaseSound, entity);

        var strength = Atmos.GetOverPressure(removed) * Atmospherics.kPaToKg_m2;

        if (strength <= 0)
            return;

        // TODO: I hate throwing system. I shouldn't need to do this boilerplate to get a nice looking throw
        var rot = _xform.GetWorldRotation(entity);
        var ang = _random.NextAngle(rot + ThrowVector.X, rot + ThrowVector.Y);

        // We bias by angle to make sure it doesn't rotate too much and flies relatively straight.
        _physics.ApplyAngularImpulse(entity, (float)(strength * ang));

        // TODO ATMOS: If we can predict ReleaseGas at some point, we should have this apply an impulse to a person holding this gas tank.
        _throwing.TryThrow(entity, ang.ToWorldVec() * strength, strength, doSpin: false);
    }

    public GasMixture RemoveAirOutput(Entity<GasTankComponent> gasTank, float volume)
    {
        var mixture = _atmosphereSystem.RemoveVolumeAtPressure(gasTank.Comp.Air, volume, gasTank.Comp.ReleasePressure);
        // We resize the volume because lungs breathe in volume rather than being pressure based atm.
        // If we don't do this, they won't consume all of the outputted gas or will consume way too much.
        mixture.Volume = volume;
        return mixture;
    }

    public GasMixture RemoveAir(Entity<GasTankComponent> gasTank, float amount)
    {
        return gasTank.Comp.Air.Remove(amount);
    }

    public void AssumeAir(Entity<GasTankComponent> ent, GasMixture giver)
    {
        _atmosphereSystem.Merge(ent.Comp.Air, giver);
        CheckStatus(ent);
    }

    protected override void SafetyMeasures(Entity<GasTankComponent> entity)
    {
        if (entity.Comp.ReleaseValveOpen)
            return;

        ToggleValve(entity);
        if (entity.Comp.SafetyAlert != null)
            _popup.PopupEntity(Loc.GetString(entity.Comp.SafetyAlert), entity, PopupType.LargeCaution);

        Dirty(entity);
    }

    /// <summary>
    /// Returns the gas mixture for the gas analyzer
    /// </summary>
    private void OnAnalyzed(EntityUid uid, GasTankComponent component, GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(uid), component.Air));
    }

    private void OnGasTankPrice(EntityUid uid, GasTankComponent component, ref PriceCalculationEvent args)
    {
        args.Price += _atmosphereSystem.GetPrice(component.Air);
    }
}
