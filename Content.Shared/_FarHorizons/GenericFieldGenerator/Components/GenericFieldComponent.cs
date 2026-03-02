using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._FarHorizons.GenericFieldGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenericFieldComponent : Component
{
    /// <summary>
    /// What made this entity?
    /// </summary>
    [ViewVariables]
    public Entity<GenericFieldGeneratorComponent>? SourceGen;
}