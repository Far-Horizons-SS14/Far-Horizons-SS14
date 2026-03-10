using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._FarHorizons.GenericFieldGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenericFieldComponent : Component
{
    /// <summary>
    /// What made this entity?
    /// </summary>
    [ViewVariables]
    public Entity<GenericFieldGeneratorComponent>? SourceGen;

    /// <summary>
    /// was a temporary tile made with this entity?
    /// </summary>
    [ViewVariables]
    public bool TempTile = false;

    /// <summary>
    /// what tile was made with the entity?
    /// </summary>
    [ViewVariables]
    public TileRef Tileref;

    /// <summary>
    /// MapGrid for tile that was made with the entity
    /// </summary>
    [ViewVariables]
    public MapGridComponent MapGrid;

    /// <summary>
    /// GridUid for tile that was made with the entity
    /// </summary>
    [ViewVariables]
    public EntityUid GridUid;
}