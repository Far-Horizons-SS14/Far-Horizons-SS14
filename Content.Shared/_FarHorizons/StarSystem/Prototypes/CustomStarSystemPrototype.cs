using System.Numerics;
using Content.Shared._FarHorizons.StarSystem.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.StarSystem.Prototypes;

[Prototype]
public sealed partial class CustomStarSystemPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField(required: true)] public CustomStar Star = default!;
    // Can later implement planets here too, but I'm just doing star for now
}

[DataDefinition, Serializable]
public sealed partial class CustomStar
{
    [DataField(required: true)] public string Shader = default!;
    [DataField] public Color Color = Color.White;
    [DataField] public Vector2 Position = Vector2.Zero;
    // I imagine custom shaders more often than not will ignore these values
    [DataField] public float Temperature = 0;
    [DataField] public float Mass = 0;
    [DataField] public float Luminocity = 0;
    [DataField] public float Radius = 0;
    [DataField] public string Name = "";

    public Star MakeStar() => new()
    {
        SolarMass = Mass,
        Luminocity = Luminocity,
        Radius = Radius,
        Temperature = Temperature,
        Shader = Shader,
        Color = Color,
        Position = Position,
        Name = Name
    };

}