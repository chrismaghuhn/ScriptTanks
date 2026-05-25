using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Geometry;

/// <summary>
/// Deterministic axis-aligned bounding box defined by two
/// <see cref="FixedVec2"/> corners. The rectangle requires a strictly
/// positive extent on both axes, exposes derived <see cref="Width"/> and
/// <see cref="Height"/>, and provides an inclusive point-in-rectangle test.
/// No collision response, clamping helpers, or rotated geometry is offered
/// in this primitive.
/// </summary>
public readonly struct FixedRect : IEquatable<FixedRect>
{
    public FixedVec2 Min { get; }

    public FixedVec2 Max { get; }

    public Fixed Width { get; }

    public Fixed Height { get; }

    public FixedRect(FixedVec2 min, FixedVec2 max)
    {
        if (max.X <= min.X)
        {
            throw new ArgumentOutOfRangeException(
                nameof(max),
                "Max.X must be greater than Min.X.");
        }

        if (max.Y <= min.Y)
        {
            throw new ArgumentOutOfRangeException(
                nameof(max),
                "Max.Y must be greater than Min.Y.");
        }

        Min = min;
        Max = max;
        Width = max.X - min.X;
        Height = max.Y - min.Y;
    }

    public static FixedRect FromMinSize(FixedVec2 min, Fixed width, Fixed height)
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

        FixedVec2 max = new FixedVec2(min.X + width, min.Y + height);
        return new FixedRect(min, max);
    }

    public bool Contains(FixedVec2 point)
        => point.X >= Min.X
           && point.X <= Max.X
           && point.Y >= Min.Y
           && point.Y <= Max.Y;

    public bool Equals(FixedRect other)
        => Min.Equals(other.Min) && Max.Equals(other.Max);

    public override bool Equals(object? obj)
        => obj is FixedRect other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Min, Max);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "FixedRect(min={0}, max={1})",
            Min,
            Max);
}
