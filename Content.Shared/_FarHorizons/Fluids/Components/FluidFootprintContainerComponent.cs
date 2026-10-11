using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Fluids.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class FluidFootprintContainerComponent : Component
{
    [ViewVariables, AutoNetworkedField] public List<Color> ColorPalette = new();
    [ViewVariables, AutoNetworkedField] public List<ProtoId<FootprintTypePrototype>> ProtoPalette = new();
    [ViewVariables, AutoNetworkedField] public List<FootprintData> Footprints = new();
    [DataField] public EntProtoId? CleanEffect;
    [DataField] public int FootprintLimit = 100; // Plain hard limit. With 8x8 footprints, assuming all drawn at 100% scale - that's 6k pixels, more than enough to cover 1k pixels of a single tile

    // I compress the data to the best of my ability without losing anything.
    // If I did math right, that's around 75%-85% savings on memory and network data as compared to doing this naively
    public void AddFootprint(
        Vector2 position,
        Angle angle,
        ProtoId<FootprintTypePrototype> footprint,
        float size,
        Color color,
        bool flip,
        float opacity)
    {
        if (Footprints.Count > FootprintLimit)
            return;

        if (!ProtoPalette.Contains(footprint))
        {
            // Bit 7 is reserved for the Flip flag, capping palette size to 128 items
            if (ProtoPalette.Count >= 128)
                return;

            ProtoPalette.Add(footprint);
        }
        
        var rawProtoId = (byte)ProtoPalette.IndexOf(footprint);
        var packedProtoAndFlip = (byte)(rawProtoId | (flip ? 0x80 : 0x00));

        if (!ColorPalette.Contains(color))
            ColorPalette.Add(color);
        
        var colorId = (byte)ColorPalette.IndexOf(color);

        // Normalize coordinates and pack down to byte (0 - 255)
        var packedPosX = (byte)MathF.Round(Math.Clamp((position.X + 0.5f) / 2.0f, 0f, 1f) * byte.MaxValue);
        var packedPosY = (byte)MathF.Round(Math.Clamp((position.Y + 0.5f) / 2.0f, 0f, 1f) * byte.MaxValue);

        // Normalize angle to 0 - 2PI in 256 discrete steps
        var theta = angle.Reduced().Theta;
        if (theta < 0)
            theta += MathF.Tau;

        var packedAngle = (byte)(Math.Clamp(theta / MathF.Tau, 0f, 1f) * byte.MaxValue);

        // Normalize size between 0 and 3
        var packedSize = (byte)(Math.Clamp(size / 3f, 0f, 1f) * byte.MaxValue);

        var packedOpacity = (byte)Math.Clamp((int)(opacity * 255f), 0, 255);

        var result = new FootprintData(packedPosX, packedPosY, packedAngle, packedProtoAndFlip, colorId, packedSize, packedOpacity);
        Footprints.Add(result);
    }

    public (
        Vector2 Position,
        Angle Angle,
        ProtoId<FootprintTypePrototype> Footprint,
        float Size,
        Color Color,
        bool Flip,
        float Opacity
        )
        Unpack(FootprintData data)
    {
        var posX = (data.PosX / (float)byte.MaxValue * 2.0f) - 0.5f;
        var posY = (data.PosY / (float)byte.MaxValue * 2.0f) - 0.5f;
        var angle = new Angle(data.Angle / (float)byte.MaxValue * MathF.Tau);
        var size = data.Size / (float)byte.MaxValue * 3f;
        
        // Unpack ProtoId (bits 0-6) and Flip (bit 7)
        var protoId = (byte)(data.ProtoAndFlip & 0x7F);
        var flip = (data.ProtoAndFlip & 0x80) != 0;

        var footprint = ProtoPalette[protoId];
        var color = ColorPalette[data.ColorId];
        var opacity = data.Opacity / 255f;

        return (new Vector2(posX, posY), angle, footprint, size, color, flip, opacity);
    }
}

[Serializable, NetSerializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FootprintData // 7 bytes baybee
(
    byte PosX,
    byte PosY,
    byte Angle,
    // 7 bits are dedicated to protoId and 1 bit is a boolean flip. 
    // This puts a limit of 128 possible different footprints per tile, but with a limit of 100 total, this shouldn't be an issue.
    byte ProtoAndFlip,
    byte ColorId,
    byte Size,
    byte Opacity
);