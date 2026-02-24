using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Verbs;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Starlight.BreathOrgan.Components;
using Content.Shared.Body;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Starlight.BreathOrgan.Systems;

/// <summary>
/// A system that adds a verb to refill organ gas tank,
/// it is required as we cannot normally access organs in our body.
/// </summary>
public sealed class OrganGasTankFillSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasCanisterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<GasCanisterComponent> canister, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        // Check if the user has organ gas tanks
        if (!TryComp<BodyComponent>(args.User, out var body))
            return;

        if (!_body.TryGetOrgansWithComponent<GasTankComponent>((args.User, body), out var allOrganTanks))
            return;
        
        var organTanks = new List<Entity<GasTankComponent, OrganGasTankFillableComponent, OrganComponent>>();
        
        foreach (var organTank in allOrganTanks)
        {
            var (tankEntity, gasTank) = organTank;
            if (!TryComp<OrganComponent>(tankEntity, out var organComp))
                continue;
            if (TryComp<OrganGasTankFillableComponent>(tankEntity, out var fillable)) 
                organTanks.Add((tankEntity, gasTank, fillable, organComp));
        }
        
        // Check if we have any organs to refill
        if (organTanks.Count == 0)
            return;

        // Check if the canister is empty
        if (canister.Comp.Air.TotalMoles <= 0)
            return;

        // Add a verb to refill the organ
        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("verb-fill-organ"),
            Act = () => FillOrganGasTanks(canister, user, organTanks),
            Priority = 1
        });
    }

    private void FillOrganGasTanks(
        Entity<GasCanisterComponent> canister,
        EntityUid user,
        List<Entity<GasTankComponent, OrganGasTankFillableComponent, OrganComponent>> organTanks)
    {
        // Check if the canister has any moles
        if (canister.Comp.Air.TotalMoles <= 0)
            return;
        
        var canisterPressure = canister.Comp.Air.Pressure;
        var filledAny = false;
        foreach (var organTank in organTanks)
        {
            var (tankEntity, gasTank, fillable, organ) = organTank;
            
            var currentPressure = gasTank.Air.Pressure;
            var targetPressure = fillable.TargetPressure;
            
            // Limit how much we can fill to the pressure in the canister (the same way gas tanks work)
            var effectiveTargetPressure = Math.Min(targetPressure, canisterPressure);
            
            if (currentPressure >= effectiveTargetPressure - 0.01f)
                continue; // Skip if we already have more gas than we could fill from the canister

            // Fill the organ
            if (_atmos.PumpGasTo(canister.Comp.Air, gasTank.Air, effectiveTargetPressure))
            {
                filledAny = true;
                EntityUid soundSource = tankEntity;
                if (organ.Body != null)
                {
                    soundSource = organ.Body.Value;
                }
                // Play the fill sound
                if (gasTank.ConnectSound != null)
                {
                    _audio.PlayPvs(gasTank.ConnectSound, soundSource);
                }
                Dirty(tankEntity, gasTank);
            }
        }

        if (filledAny)
        {
            Dirty(canister);
        }
    }
}
