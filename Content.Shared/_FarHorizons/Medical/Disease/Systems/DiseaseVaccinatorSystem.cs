using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared.Interaction;

namespace Content.Shared._FarHorizons.Medical.Disease.Systems;

public sealed class DiseaseVaccinatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseVaccinatorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<DiseaseVaccinatorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        if(!TryComp<DiseaseSampleComponent>(args.Used, out var sample)) return;

        
    }
}