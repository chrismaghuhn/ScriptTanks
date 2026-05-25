using System;
using System.Globalization;

namespace ScriptTanks.Core.Math;

/// <summary>
/// Deterministic 2D vector built on top of <see cref="Fixed"/>. Component-wise
/// operations delegate to <see cref="Fixed"/>, so multiplication, division and
/// ratio construction inherit the <see cref="Int128"/> deterministic path.
/// No floating-point math is used. No square root operation is provided;
/// callers must use <see cref="LengthSquared"/> or <see cref="DistanceSquaredTo"/>.
/// </summary>
public readonly struct FixedVec2 : IEquatable<FixedVec2>
{
    public Fixed X { get; }

    public Fixed Y { get; }

    public static FixedVec2 Zero { get; } = new FixedVec2(Fixed.Zero, Fixed.Zero);

    public FixedVec2(Fixed x, Fixed y)
    {
        X = x;
        Y = y;
    }

    public static FixedVec2 FromRaw(long rawX, long rawY)
        => new FixedVec2(Fixed.FromRaw(rawX), Fixed.FromRaw(rawY));

    public static FixedVec2 FromInts(int x, int y)
        => new FixedVec2(Fixed.FromInt(x), Fixed.FromInt(y));

    public Fixed LengthSquared() => X * X + Y * Y;

    public Fixed DistanceSquaredTo(FixedVec2 other)
    {
        Fixed dx = other.X - X;
        Fixed dy = other.Y - Y;
        return dx * dx + dy * dy;
    }

    public static FixedVec2 operator +(FixedVec2 left, FixedVec2 right)
        => new FixedVec2(left.X + right.X, left.Y + right.Y);

    public static FixedVec2 operator -(FixedVec2 left, FixedVec2 right)
        => new FixedVec2(left.X - right.X, left.Y - right.Y);

    public static FixedVec2 operator -(FixedVec2 value)
        => new FixedVec2(-value.X, -value.Y);

    public static FixedVec2 operator *(FixedVec2 vector, Fixed scalar)
        => new FixedVec2(vector.X * scalar, vector.Y * scalar);

    public static FixedVec2 operator *(Fixed scalar, FixedVec2 vector)
        => new FixedVec2(scalar * vector.X, scalar * vector.Y);

    public static FixedVec2 operator /(FixedVec2 vector, Fixed scalar)
        => new FixedVec2(vector.X / scalar, vector.Y / scalar);

    public static bool operator ==(FixedVec2 left, FixedVec2 right) => left.Equals(right);

    public static bool operator !=(FixedVec2 left, FixedVec2 right) => !left.Equals(right);

    public bool Equals(FixedVec2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is FixedVec2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X.Raw, Y.Raw);

    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
}
