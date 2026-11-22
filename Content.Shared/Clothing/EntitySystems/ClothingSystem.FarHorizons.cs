using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public abstract partial class ClothingSystem
{
    public void SetLayerRSI(ClothingComponent clothing, string slot, string mapKey, string rsi, string? state=null)
    {
        foreach (var layer in clothing.ClothingVisuals[slot])
        {
            if (layer.MapKeys == null)
                return;

            if (!layer.MapKeys.Contains(mapKey))
                continue;

            layer.State = state ?? layer.State;
            layer.RsiPath = rsi;
        }
    }

    public void SetLayerVisibility(ClothingComponent clothing, string slot, string mapKey, bool Visible)
    {
        foreach (var layer in clothing.ClothingVisuals[slot])
        {
            if (layer.MapKeys == null)
                return;

            if (!layer.MapKeys.Contains(mapKey))
                continue;

            layer.Visible = Visible;
        }
    }
}