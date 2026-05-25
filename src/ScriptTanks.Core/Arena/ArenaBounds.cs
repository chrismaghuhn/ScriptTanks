using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Arena;

/// <summary>
/// Deterministic rectangular world bounds. Defines an arena from
/// <c>(0, 0)</c> to <c>(Width, Height)</c> and provides an inclusive
/// inside-check for a single point. No collision, clamping, or movement
/// blocking is performed in this primitive.
/// </summary>
public readonly struct ArenaBounds : IEquatable<ArenaBounds>
{
    public Fixed Width { get; }

    public Fixed Height { get; }

    public FixedVec2 Min { get; }

    public FixedVec2 Max { get; }

    public ArenaBounds(Fixed width, Fixed height)
    {
        if (width <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Width must be positive.");
        }

        if (height <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Height must be positive.");
        }

        Width = width;
        Height = height;
        Min = FixedVec2.Zero;
        Max = new FixedVec2(width, height);
    }

    public bool Contains(FixedVec2 position)
        => position.X >= Fixed.Zero
           && position.Y >= Fixed.Zero
           && position.X <= Width
           && position.Y <= Height;

    public bool Equals(ArenaBounds other)
        => Width.Equals(other.Width) && Height.Equals(other.Height);

    public override bool Equals(object? obj)
        => obj is ArenaBounds other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Width, Height);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "ArenaBounds(width={0}, height={1})",
            Width,
            Height);
}
