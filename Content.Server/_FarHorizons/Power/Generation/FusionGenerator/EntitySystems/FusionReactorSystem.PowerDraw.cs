using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem
{
    #region Power Draw API
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

        /// There's probably a good way to condense these three into one function, but I'm hesitant 
        /// to do it in case it breaks something

        void Surplus()
        {
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

            foreach (var draw in drawComponents)
            {
                draw.Satisfaction = 1;
                draw.OnSatisfy?.Invoke();
            }

            foreach (var supplier in supplyComponents)
            {
                supplier.Surplus = emptyCapacity <= 0 ? supplier.Supply / powerSupply * netPower : 0;
                supplier.OnSurplus?.Invoke();
            }
        }

        void Zero()
        {

            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                comp.Demand = comp.Supply = 0;
                _battery.RefreshChargeRate(uid);
            }

            foreach (var draw in drawComponents)
            {
                draw.Satisfaction = 1;
                draw.OnSatisfy?.Invoke();
            }

            foreach (var supplier in supplyComponents)
            {
                supplier.Surplus = 0;
                supplier.OnSurplus?.Invoke();
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

            var suppliedWatts = Math.Min(storedWatts + powerSupply, powerDraw);
            var fromBatteries = suppliedWatts - powerSupply;

            foreach (var (uid, comp) in fusionReactor.Batteries)
            {
                comp.Supply = 0;
                comp.Demand = comp.NetBattery.CanDischarge && storedWatts != 0 ? comp.Cache / storedWatts * fromBatteries : 0;

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
                supplier.OnSurplus?.Invoke();
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
    public void SetPowerDraw(Entity<FusionReactorPowerDrawComponent?> ent, bool enable, float? amount = null)
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
    public float GetPowerSupplied(Entity<FusionReactorPowerDrawComponent?> ent) =>
        !Resolve(ent, ref ent.Comp, false) ? 0 : ent.Comp.Supplied;

    /// <summary>
    /// Gets the power supplied to the device as a fraction of requested power
    /// </summary>
    /// <returns>Satisfaction, between 0 and 1</returns>
    public float GetPowerSatisfaction(Entity<FusionReactorPowerDrawComponent?> ent) =>
        !Resolve(ent, ref ent.Comp, false) ? 0 : ent.Comp.Satisfaction;

    /// <summary>
    /// Sets the action to perform after the satisfaction has been calculated
    /// </summary>
    public void SetOnSatisfy(Entity<FusionReactorPowerDrawComponent?> ent, Action action)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.OnSatisfy = action;
    }

    /// <summary>
    /// Enables/disables the power supply of a device <paramref name="ent"/>. If <paramref name="amount"/> 
    /// is null, the supply will be unchanged.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="enable"></param>
    /// <param name="amount">Power draw, in watts</param>
    public void SetPowerSupply(Entity<FusionReactorPowerSupplyComponent?> ent, bool enable, float? amount = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enable;
        if (amount.HasValue)
            ent.Comp.Supply = amount.Value;
    }

    /// <summary>
    /// Gets the surplus power made by the device in watts
    /// </summary>
    /// <returns>Power surplus, in watts</returns>
    public float GetPowerSurplus(Entity<FusionReactorPowerSupplyComponent?> ent) =>
        !Resolve(ent, ref ent.Comp, false) ? 0 : ent.Comp.Surplus;

    /// <summary>
    /// Sets the action to perform after the surplus has been calculated
    /// </summary>
    public void SetOnSurplus(Entity<FusionReactorPowerSupplyComponent?> ent, Action action)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.OnSurplus = action;
    }
    #endregion
}