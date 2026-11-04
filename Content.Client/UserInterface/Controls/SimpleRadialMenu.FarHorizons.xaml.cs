using System.Linq;

namespace Content.Client.UserInterface.Controls;

public sealed partial class SimpleRadialMenu
{
    private Color _backgroundColorSrgb = new(70, 73, 102, 128);
    private Color _hoverBackgroundColorSrgb = new(87, 91, 127, 128);
    private readonly List<(RadialMenuButtonWithSector Button, RadialMenuOptionBase Model)> _sectorButtons = new();

    /// <summary>
    /// Updates existing sector buttons' colors from a fresh model list, matched by
    /// build order, without tearing down/rebuilding the menu (avoids resetting
    /// nested-layer visibility). Falls back to a full <see cref="SetButtons"/> rebuild
    /// if the model structure doesn't match what's currently built (e.g. items were
    /// added/removed).
    /// </summary>
    public void UpdateBackgroundColors(IEnumerable<RadialMenuOptionBase> models, SimpleRadialMenuSettings? settings = null)
    {
        var flatModels = FlattenModels(models).ToList();

        if (flatModels.Count != _sectorButtons.Count)
        {
            SetButtons(models, settings);
            return;
        }

        for (var i = 0; i < flatModels.Count; i++)
        {
            var model = flatModels[i];
            var (button, _) = _sectorButtons[i];

            button.BackgroundColor = model.BackgroundColor ?? _backgroundColorSrgb;
            button.HoverBackgroundColor = model.HoverBackgroundColor ?? _hoverBackgroundColorSrgb;
            _sectorButtons[i] = (button, model);

            button.InvalidateMeasure();
        }
    }

    // Mirrors the traversal order Fill/RecursiveContainerExtraction build buttons in
    // (children before their own nested-layer link button), so index i here lines up
    // with index i in _sectorButtons.
    private static IEnumerable<RadialMenuOptionBase> FlattenModels(IEnumerable<RadialMenuOptionBase> models)
    {
        foreach (var model in models)
        {
            if (model is RadialMenuNestedLayerOption nested)
            {
                foreach (var child in FlattenModels(nested.Nested))
                    yield return child;
                yield return model;
            }
            else
            {
                yield return model;
            }
        }
    }
}

