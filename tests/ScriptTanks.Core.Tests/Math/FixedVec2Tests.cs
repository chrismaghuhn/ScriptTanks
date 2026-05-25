using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedVec2Tests
{
    [Fact]
    public void Zero_HasBothComponentsZero()
    {
        Assert.Equal(Fixed.Zero, FixedVec2.Zero.X);
        Assert.Equal(Fixed.Zero, FixedVec2.Zero.Y);
    }

    [Fact]
    public void Constructor_PreservesComponents()
    {
        Fixed x = Fixed.FromInt(7);
        Fixed y = Fixed.FromRatio(-3, 2);
        FixedVec2 v = new FixedVec2(x, y);

        Assert.Equal(x, v.X);
        Assert.Equal(y, v.Y);
    }

    [Fact]
    public void FromRaw_PreservesRawValuesBitForBit()
    {
        FixedVec2 v = FixedVec2.FromRaw(123, 456);

        Assert.Equal(123, v.X.Raw);
        Assert.Equal(456, v.Y.Raw);
    }

    [Fact]
    public void FromInts_ScalesBothComponents()
    {
        FixedVec2 v = FixedVec2.FromInts(1, 2);

        Assert.Equal(Fixed.FromInt(1), v.X);
        Assert.Equal(Fixed.FromInt(2), v.Y);
    }

    [Fact]
    public void Addition_IsComponentWise()
    {
        FixedVec2 a = FixedVec2.FromInts(1, 2);
        FixedVec2 b = FixedVec2.FromInts(3, 4);

        Assert.Equal(FixedVec2.FromInts(4, 6), a + b);
    }

    [Fact]
    public void Subtraction_IsComponentWise()
    {
        FixedVec2 a = FixedVec2.FromInts(5, 7);
        FixedVec2 b = FixedVec2.FromInts(2, 3);

        Assert.Equal(FixedVec2.FromInts(3, 4), a - b);
    }

    [Fact]
    public void UnaryNegation_FlipsBothComponents()
    {
        Assert.Equal(FixedVec2.FromInts(-3, 4), -FixedVec2.FromInts(3, -4));
        Assert.Equal(FixedVec2.Zero, -FixedVec2.Zero);
    }

    [Fact]
    public void VectorTimesScalar_IsComponentWise()
    {
        FixedVec2 v = FixedVec2.FromInts(2, 3);

        Assert.Equal(FixedVec2.FromInts(4, 6), v * Fixed.FromInt(2));
        Assert.Equal(FixedVec2.FromInts(-4, -6), v * Fixed.FromInt(-2));
        Assert.Equal(FixedVec2.Zero, v * Fixed.Zero);
    }

    [Fact]
    public void ScalarTimesVector_IsSymmetric()
    {
        FixedVec2 v = FixedVec2.FromInts(2, 3);
        Fixed s = Fixed.FromInt(2);

        Assert.Equal(v * s, s * v);
    }

    [Fact]
    public void VectorDividedByScalar_IsComponentWise()
    {
        FixedVec2 v = FixedVec2.FromInts(4, 6);

        Assert.Equal(FixedVec2.FromInts(2, 3), v / Fixed.FromInt(2));
        Assert.Equal(FixedVec2.FromInts(-2, -3), v / Fixed.FromInt(-2));
    }

    [Fact]
    public void Division_ByZeroScalar_Throws()
    {
        Assert.Throws<DivideByZeroException>(
            () => FixedVec2.FromInts(1, 2) / Fixed.Zero);
    }

    [Fact]
    public void LengthSquared_ReturnsXSquaredPlusYSquared()
    {
        Assert.Equal(Fixed.FromInt(25), FixedVec2.FromInts(3, 4).LengthSquared());
        Assert.Equal(Fixed.Zero, FixedVec2.Zero.LengthSquared());
    }

    [Fact]
    public void LengthSquared_HandlesNegativeComponents()
    {
        Assert.Equal(Fixed.FromInt(25), FixedVec2.FromInts(-3, -4).LengthSquared());
        Assert.Equal(Fixed.FromInt(25), FixedVec2.FromInts(-3, 4).LengthSquared());
    }

    [Fact]
    public void DistanceSquaredTo_ReturnsExpectedValue()
    {
        FixedVec2 a = FixedVec2.FromInts(1, 2);
        FixedVec2 b = FixedVec2.FromInts(4, 6);

        Assert.Equal(Fixed.FromInt(25), a.DistanceSquaredTo(b));
    }

    [Fact]
    public void DistanceSquaredTo_IsSymmetric()
    {
        FixedVec2 a = FixedVec2.FromInts(1, 2);
        FixedVec2 b = FixedVec2.FromInts(4, 6);

        Assert.Equal(a.DistanceSquaredTo(b), b.DistanceSquaredTo(a));
    }

    [Fact]
    public void DistanceSquaredTo_ToSelf_IsZero()
    {
        FixedVec2 a = FixedVec2.FromInts(7, -3);

        Assert.Equal(Fixed.Zero, a.DistanceSquaredTo(a));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        Assert.True(FixedVec2.FromInts(1, 2) == FixedVec2.FromInts(1, 2));
        Assert.False(FixedVec2.FromInts(1, 2) == FixedVec2.FromInts(1, 3));
        Assert.False(FixedVec2.FromInts(1, 2) == FixedVec2.FromInts(2, 2));
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        Assert.False(FixedVec2.FromInts(1, 2) != FixedVec2.FromInts(1, 2));
        Assert.True(FixedVec2.FromInts(1, 2) != FixedVec2.FromInts(1, 3));
    }

    [Fact]
    public void Equals_FixedVec2_AndObject_Work()
    {
        FixedVec2 a = FixedVec2.FromInts(3, 5);
        FixedVec2 b = FixedVec2.FromInts(3, 5);
        FixedVec2 c = FixedVec2.FromInts(3, 6);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals("not a FixedVec2"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        FixedVec2 a = FixedVec2.FromInts(3, 5);
        FixedVec2 b = FixedVec2.FromInts(3, 5);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DiffersForTrivialCounterExamples()
    {
        // Not strictly required by hash contract, but a useful smoke check
        // that the combine actually mixes both components.
        Assert.NotEqual(
            FixedVec2.FromInts(1, 2).GetHashCode(),
            FixedVec2.FromInts(2, 1).GetHashCode());
    }

    [Fact]
    public void ToString_ContainsBothComponents_SmokeOnly()
    {
        string text = FixedVec2.FromInts(1, 2).ToString();

        Assert.Contains("1", text);
        Assert.Contains("2", text);
    }
}
