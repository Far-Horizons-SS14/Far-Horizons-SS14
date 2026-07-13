using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    private void BatteryInitialize()
    {
        SubscribeLocalEvent<FusionReactorBatteryComponent, RefreshChargeRateEvent>(OnBatteryRefreshChargeRate);
        SubscribeLocalEvent<FusionReactorBatteryComponent, ComponentStartup>(OnBatteryStartup);
        // SubscribeLocalEvent<FusionReactorBatteryComponent, ComponentRemove>(OnDrawRemove);
    }

    /// TODO: Make this continuous

    /// <summary>
    /// Changes the energy stored in the <paramref name="fusionReactor"/> by the <paramref name="amount"/>. 
    /// Positive values will add energy, negative values remove energy.
    /// </summary>
    /// <param name="fusionReactor"></param>
    /// <param name="amount"></param>
    /// <returns>Absolute value of the number of joules added or removed from the capacitors.</returns>
    [Obsolete("Use SetPowerDraw instead")]
    private float ChangePower(FusionReactorNodeGroup fusionReactor, float amount)
    {
        if (fusionReactor.Batteries.Count <= 0)
            return 0;

        var dE = amount / fusionReactor.Batteries.Count;
        var output = 0f;
        var remainder = 0f;

        foreach (var (uid, comp) in fusionReactor.Batteries)
        {
            var chargeChange = _battery.ChangeCharge(uid, dE);
            chargeChange += _battery.ChangeCharge(uid, remainder);
            remainder += dE - chargeChange;
            output += chargeChange;
        }

        return Math.Abs(output);
    }

    private void OnBatteryRefreshChargeRate(EntityUid uid, FusionReactorBatteryComponent comp, ref RefreshChargeRateEvent args)
    {
        if (comp.NetBattery.CanCharge)
            args.NewChargeRate += comp.Supply;

        if (comp.NetBattery.CanDischarge)
            args.NewChargeRate -= comp.Demand;
    }

    private void OnBatteryStartup(EntityUid uid, FusionReactorBatteryComponent comp, ref ComponentStartup args)
    {
        comp.NetBattery = EnsureComp<PowerNetworkBatteryComponent>(uid);
        comp.Battery = EnsureComp<BatteryComponent>(uid);
    }

    #region Power Draw
    private void ProcessPowerDraws(FusionReactorNodeGroup fusionReactor, float dt)
    {
        var powerDraw = 0f;
        var powerSupply = 0f;
        List<FusionReactorPowerDrawComponent> drawComponents = [];
        List<FusionReactorPowerSupplyComponent> supplyComponents = [];
        foreach (var node in fusionReactor.Nodes)
        {
            if (TryComp(node.Owner, out FusionReactorPowerDrawComponent? powerDrawComponent))
            {
                if (powerDrawComponent.Enabled && powerDrawComponent.Draw > 0)
                {
                    drawComponents.Add(powerDrawComponent);
                    powerDraw += powerDrawComponent.Draw;
                }
            }

            if (TryComp(node.Owner, out FusionReactorPowerSupplyComponent? powerSupplyComponent))
            {
                if (powerSupplyComponent.Enabled && powerSupplyComponent.Supply > 0)
                {
                    supplyComponents.Add(powerSupplyComponent);
                    powerSupply += powerSupplyComponent.Supply;
                }
            }
        }

        drawComponents.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var netPower = powerSupply - powerDraw;
        if (MathHelper.CloseTo(netPower, 0))
        {
            Zero();
        }
        else if (netPower > 0)
        {
            Surplus();
        }
        else if (netPower < 0)
        {
            Deficit();
        }

        return;

        void Surplus()
        {
            foreach (var draw in drawComponents)
            {
                draw.Satisfaction = 1;
                draw.OnSatisfy?.Invoke();
            }

            var emptyCapacity = 0f;
            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                if (!comp.NetBattery.CanCharge)
                    continue;

                comp.Cache = comp.Battery.MaxCharge - _battery.GetCharge(uid);
                emptyCapacity += comp.Cache;
            }

            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                comp.Demand = 0;
                comp.Supply = (comp.NetBattery.CanCharge && comp.Cache > 0 && emptyCapacity > 0) ? comp.Cache / emptyCapacity * netPower : 0;

                _battery.RefreshChargeRate(uid);
            }

            if (emptyCapacity <= 0)
            {
                foreach (var supplier in supplyComponents)
                {
                    supplier.Surplus = supplier.Supply / powerSupply * netPower;
                }
            }
        }

        void Zero()
        {
            foreach (var draw in drawComponents)
            {
                draw.Satisfaction = 1;
                draw.OnSatisfy?.Invoke();
            }

            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                comp.Demand = comp.Supply = 0;
                _battery.RefreshChargeRate(uid);
            }

            foreach (var supplier in supplyComponents)
            {
                supplier.Surplus = 0;
            }
        }

        void Deficit()
        {
            var storedWatts = 0f;
            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                if (!comp.NetBattery.CanDischarge)
                    continue;

                comp.Cache = _battery.GetCharge(uid) / dt;
                storedWatts += comp.Cache;
            }

            var suppliedWatts = Math.Min(storedWatts, powerDraw);

            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                comp.Supply = 0;
                comp.Demand = comp.NetBattery.CanDischarge && storedWatts != 0 ? comp.Cache / storedWatts * suppliedWatts : 0;

                _battery.RefreshChargeRate(uid);
            }

            foreach (var draw in drawComponents)
            {
                var satisfaction = suppliedWatts / draw.Draw;
                draw.Satisfaction = Math.Clamp(satisfaction, 0, 1);
                draw.OnSatisfy?.Invoke();
                suppliedWatts -= draw.Draw;
            }

            foreach (var supplier in supplyComponents)
            {
                supplier.Surplus = 0;
            }
        }
    }

    /// <summary>
    /// Enables/disables the power draw of a device <paramref name="ent"/>. If <paramref name="amount"/> 
    /// is null, the draw will be unchanged.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="enable"></param>
    /// <param name="amount">Power draw, in watts</param>
    private void SetPowerDraw(Entity<FusionReactorPowerDrawComponent?> ent, bool enable, float? amount = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enable;
        if (amount.HasValue)
            ent.Comp.Draw = amount.Value;
    }

    /// <summary>
    /// Gets the power supplied to the device in watts
    /// </summary>
    /// <returns>Power supplied, in watts</returns>
    private float GetPowerSupplied(Entity<FusionReactorPowerDrawComponent?> ent) =>
        !Resolve(ent, ref ent.Comp, false) ? 0 : ent.Comp.Supplied;

    /// <summary>
    /// Gets the power supplied to the device as a fraction of requested power
    /// </summary>
    /// <returns>Satisfaction, between 0 and 1</returns>
    private float GetPowerSatisfaction(Entity<FusionReactorPowerDrawComponent?> ent) =>
        !Resolve(ent, ref ent.Comp, false) ? 0 : ent.Comp.Satisfaction;

    /// <summary>
    /// Sets the action to perform after the satisfaction has been calculated
    /// </summary>
    private void SetOnSatisfy(Entity<FusionReactorPowerDrawComponent?> ent, Action action)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.OnSatisfy = action;
    }
    #endregion
}