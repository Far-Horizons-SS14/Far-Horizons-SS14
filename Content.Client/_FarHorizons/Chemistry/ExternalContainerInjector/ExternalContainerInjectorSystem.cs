using Content.Client.Items;
using Content.Shared._Starlight.Chemistry.ExternalContainerInjector;

namespace Content.Client._Starlight.Chemistry.ExternalContainerInjector;

public sealed partial class ExternalContainerInjectorSystem : SharedExternalContainerInjectorSystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<ExternalContainerInjectorComponent>(ent => new ExternalContainerInjectorStatusControl(ent));
    }
}