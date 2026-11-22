using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.PowerArmor;

public abstract partial class SharedPowerArmorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnModuleInstalled(Entity<PowerArmorModuleComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if(TerminatingOrDeleted(args.Entity) 
        || !TryComp<PowerArmorComponent>(args.Container.Owner, out var PAComp)
        || !TryComp<PowerCellDrawComponent>(args.Container.Owner, out var drawComp)) 
            return;
        PAComp.Modules.Add(ent.Owner);
        Dirty(args.Container.Owner, PAComp);
        InstallModule(ent, args.Container.Owner);
        if(ent.Comp.IdlePowerDrain > 0)
            _powerCell.SetDrawRate(args.Container.Owner, drawComp.DrawRate + ent.Comp.IdlePowerDrain);
    }

    [SubscribeLocalEvent]
    private void OnModuleUninstalled(Entity<PowerArmorModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if(TerminatingOrDeleted(args.Entity) 
        || !TryComp<PowerArmorComponent>(args.Container.Owner, out var PAComp)
        || !TryComp<PowerCellDrawComponent>(args.Container.Owner, out var drawComp)) 
            return;
        UninstallModule(ent, args.Container.Owner);
        if(PAComp.Modules.Contains(ent.Owner))
        {
            PAComp.Modules.Remove(ent.Owner);
            Dirty(args.Container.Owner, PAComp);
        }

         if(ent.Comp.IdlePowerDrain > 0)
            _powerCell.SetDrawRate(args.Container.Owner, drawComp.DrawRate - ent.Comp.IdlePowerDrain);

        if(ent.Comp.isEnabled) ent.Comp.isEnabled = false;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnModuleInteract(Entity<PowerArmorModuleComponent> ent, ref AfterInteractEvent args)
    {

        if (args.Target is not { } powerArmor 
        || !TryComp<WiresPanelComponent>(powerArmor, out var wires) 
        || !TryComp<PowerArmorComponent>(powerArmor, out var paComp))
        {
            args.Handled = true;
            return;
        }

        if (!wires.Open)
        {
            _popUp.PopupEntity("Open maintenance panel first.", args.User);
            args.Handled = true;
            return;
        }

        var moduleMeta = MetaData(ent.Owner);
        var totalComplexity = 0;
        foreach (var module in paComp.Modules)
        {
            var otherProto = MetaData(module).EntityPrototype;
            if (!TryComp<PowerArmorModuleComponent>(module, out var pamComp) || otherProto == null)
                continue;

            if (otherProto.ID == moduleMeta.EntityPrototype?.ID)
            {
                _popUp.PopupEntity("This module is already installed.", args.User);
                args.Handled = true;
                return;
            }
            totalComplexity+=pamComp.ComplexityCost;
        }

        if(totalComplexity > paComp.MaxComplexity)
        {
            _popUp.PopupEntity("Unable to install due exceeding modding capacity.", args.User);
            args.Handled = true;
            return;
        }

        if (_container.TryGetContainer(powerArmor, "modules", out var moduleContainer))
        {
            _container.Insert(ent.Owner, moduleContainer);
            args.Handled = true;
        }
    }

    private void InstallModule(Entity<PowerArmorModuleComponent> module, EntityUid PowerArmor)
    {
        if(!TryComp<PowerCellDrawComponent>(PowerArmor, out var drawComp)
            || !TryComp<PowerArmorComponent>(PowerArmor, out var PAComp)) return;

        if(module.Comp.ActivePowerDrain > 0 && module.Comp.isEnabled)
            _powerCell.SetDrawRate(PowerArmor, drawComp.DrawRate + module.Comp.ActivePowerDrain);

        if (module.Comp.addToOtherHalf)
        {
            if (!Exists(PAComp.OtherHalf))
                return;
            PowerArmor = PAComp.OtherHalf;
        }

        if(TryComp<PowerArmorPassiveModuleComponent>(module.Owner, out var components) && components.Components != null)
            EntityManager.AddComponents(PowerArmor, components.Components);

        Dirty(module);
    }

    private void UninstallModule(Entity<PowerArmorModuleComponent> module, EntityUid PowerArmor)
    {
        if(!TryComp<PowerCellDrawComponent>(PowerArmor, out var drawComp)
            || !TryComp<PowerArmorComponent>(PowerArmor, out var PAComp)) return;

        if (module.Comp.ActivePowerDrain > 0 && module.Comp.isEnabled)
            _powerCell.SetDrawRate(PowerArmor, drawComp.DrawRate - module.Comp.ActivePowerDrain);

        if (module.Comp.addToOtherHalf)
        {
            if (!Exists(PAComp.OtherHalf))
                return;
            PowerArmor = PAComp.OtherHalf;
        }

        if (TryComp<PowerArmorPassiveModuleComponent>(module.Owner, out var components) && components.Components != null)
            EntityManager.RemoveComponents(PowerArmor, components.Components);

        Dirty(module);
    }
}