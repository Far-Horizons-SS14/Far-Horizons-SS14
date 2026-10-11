using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server._FarHorizons.Maps.NameGenerators;

[UsedImplicitly]
public sealed partial class NeosolNameGenerator : StationNameGenerator
{
    /// <summary>
    ///     Where the map comes from. Should be a two or three letter code, for example "VG" for Packedstation.
    /// </summary>
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "NS";
    private string[] SuffixCodes => new []{ "XS", "GU", "MQ", "SC", "NF" }; // idk what they meant originally, but I just wrote random letters

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        // No way in hell am I writing custom format code just to add nice names. You can live with {0}
        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 999):D3}");
    }
}
