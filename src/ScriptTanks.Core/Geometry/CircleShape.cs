using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Geometry;

/// <summary>
/// Deterministic circle defined by a <see cref="FixedVec2"/> center and a
/// strictly positive <see cref="Fixed"/> radius. <see cref="Contains"/> uses
/// squared distance so no <c>Math.Sqrt</c> call is required, and the rim
/// itself counts as inside (inclusive boundary).
/// </summary>
public readonly struct CircleShape : IEquatable<CircleShape>
{
    public FixedVec2 Center { get; }

    public Fixed Radius { get; }

    public CircleShape(FixedVec2 center, Fixed radius)
    {
        if (radius <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Radius must be positive.");
        }

        Center = center;
        Radius = radius;
    }

    public bool Contains(FixedVec2 point)
        => Center.DistanceSquaredTo(point) <= Radius * Radius;

    public bool Equals(CircleShape other)
        => Center.Equals(other.Center) && Radius.Equals(other.Radius);

    public override bool Equals(object? obj)
        => obj is CircleShape other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Center, Radius);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "CircleShape(center={0}, radius={1})",
            Center,
            Radius);
}
