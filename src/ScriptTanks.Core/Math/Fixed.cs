using System;
using System.Globalization;

namespace ScriptTanks.Core.Math;

/// <summary>
/// Deterministic fixed-point-light numeric type. One world unit equals
/// <see cref="Scale"/> raw units. All arithmetic is integer based and
/// uses <see cref="Int128"/> internally for multiplication, division and
/// ratio construction to guarantee platform-identical results without
/// floating-point drift.
/// </summary>
public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
{
    public const long Scale = 1000;

    public long Raw { get; }

    private Fixed(long raw)
    {
        Raw = raw;
    }

    public static Fixed Zero { get; } = new Fixed(0);

    public static Fixed One { get; } = new Fixed(Scale);

    public static Fixed FromRaw(long raw) => new Fixed(raw);

    public static Fixed FromInt(int value) => new Fixed((long)value * Scale);

    public static Fixed FromRatio(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException(
                "Fixed.FromRatio denominator must not be zero.");
        }

        Int128 raw = (Int128)numerator * Scale / denominator;
        return new Fixed((long)raw);
    }

    public int ToIntTruncated() => (int)(Raw / Scale);

    public double ToDouble() => (double)Raw / Scale;

    public static Fixed operator +(Fixed left, Fixed right)
        => new Fixed(left.Raw + right.Raw);

    public static Fixed operator -(Fixed left, Fixed right)
        => new Fixed(left.Raw - right.Raw);

    public static Fixed operator -(Fixed value)
        => new Fixed(-value.Raw);

    public static Fixed operator *(Fixed left, Fixed right)
    {
        Int128 raw = (Int128)left.Raw * right.Raw / Scale;
        return new Fixed((long)raw);
    }

    public static Fixed operator /(Fixed left, Fixed right)
    {
        if (right.Raw == 0)
        {
            throw new DivideByZeroException(
                "Fixed division by zero is not allowed.");
        }

        Int128 raw = (Int128)left.Raw * Scale / right.Raw;
        return new Fixed((long)raw);
    }

    public static bool operator ==(Fixed left, Fixed right) => left.Raw == right.Raw;

    public static bool operator !=(Fixed left, Fixed right) => left.Raw != right.Raw;

    public static bool operator <(Fixed left, Fixed right) => left.Raw < right.Raw;

    public static bool operator >(Fixed left, Fixed right) => left.Raw > right.Raw;

    public static bool operator <=(Fixed left, Fixed right) => left.Raw <= right.Raw;

    public static bool operator >=(Fixed left, Fixed right) => left.Raw >= right.Raw;

    public bool Equals(Fixed other) => Raw == other.Raw;

    public override bool Equals(object? obj) => obj is Fixed other && Equals(other);

    public override int GetHashCode() => Raw.GetHashCode();

    public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);

    public override string ToString()
        => Raw.ToString("D", CultureInfo.InvariantCulture);
}
