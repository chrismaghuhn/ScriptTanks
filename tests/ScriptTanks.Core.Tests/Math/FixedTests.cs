using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedTests
{
    [Fact]
    public void Scale_ShouldBe_1000()
    {
        Assert.Equal(1000, Fixed.Scale);
    }

    [Fact]
    public void Zero_HasRawZero()
    {
        Assert.Equal(0, Fixed.Zero.Raw);
    }

    [Fact]
    public void One_HasRawEqualToScale()
    {
        Assert.Equal(Fixed.Scale, Fixed.One.Raw);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(1234L)]
    [InlineData(-9876L)]
    public void FromRaw_PreservesRawBitForBit(long raw)
    {
        Assert.Equal(raw, Fixed.FromRaw(raw).Raw);
    }

    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 1000L)]
    [InlineData(2, 2000L)]
    [InlineData(-1, -1000L)]
    [InlineData(-7, -7000L)]
    public void FromInt_ScalesByScale(int value, long expectedRaw)
    {
        Assert.Equal(expectedRaw, Fixed.FromInt(value).Raw);
    }

    [Theory]
    [InlineData(1, 2, 500L)]
    [InlineData(3, 2, 1500L)]
    [InlineData(-1, 2, -500L)]
    [InlineData(7, 4, 1750L)]
    [InlineData(-7, 4, -1750L)]
    [InlineData(1, 3, 333L)]
    [InlineData(-1, 3, -333L)]
    public void FromRatio_ProducesDeterministicRaw(int numerator, int denominator, long expectedRaw)
    {
        Assert.Equal(expectedRaw, Fixed.FromRatio(numerator, denominator).Raw);
    }

    [Fact]
    public void FromRatio_DenominatorZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Fixed.FromRatio(1, 0));
    }

    [Fact]
    public void Addition_Works()
    {
        Assert.Equal(Fixed.FromInt(5), Fixed.FromInt(2) + Fixed.FromInt(3));
        Assert.Equal(Fixed.FromInt(0), Fixed.FromInt(2) + Fixed.FromInt(-2));
    }

    [Fact]
    public void Subtraction_Works()
    {
        Assert.Equal(Fixed.FromInt(3), Fixed.FromInt(5) - Fixed.FromInt(2));
        Assert.Equal(Fixed.FromInt(-1), Fixed.FromInt(2) - Fixed.FromInt(3));
    }

    [Fact]
    public void UnaryNegation_Works()
    {
        Assert.Equal(Fixed.FromInt(-4), -Fixed.FromInt(4));
        Assert.Equal(Fixed.FromInt(4), -Fixed.FromInt(-4));
        Assert.Equal(Fixed.Zero, -Fixed.Zero);
    }

    [Fact]
    public void Multiplication_Works()
    {
        Assert.Equal(Fixed.FromInt(6), Fixed.FromInt(3) * Fixed.FromInt(2));
        Assert.Equal(Fixed.FromInt(-6), Fixed.FromInt(3) * Fixed.FromInt(-2));
        Assert.Equal(Fixed.Zero, Fixed.FromInt(7) * Fixed.Zero);
        Assert.Equal(Fixed.FromRatio(3, 2), Fixed.FromInt(3) * Fixed.FromRatio(1, 2));
    }

    [Fact]
    public void Multiplication_UsesInt128_NoOverflowForLargeValues()
    {
        // Raw values: 10_000_000 * Scale = 1e10 each.
        // long * long would compute 1e20 which overflows long.MaxValue (~9.2e18).
        // Int128 path keeps the result deterministic and lossless.
        Fixed a = Fixed.FromInt(10_000_000);
        Fixed b = Fixed.FromInt(10_000_000);
        Fixed product = a * b;
        long expectedRaw = 10_000_000L * 10_000_000L * Fixed.Scale;
        Assert.Equal(expectedRaw, product.Raw);
    }

    [Fact]
    public void Division_Works()
    {
        Assert.Equal(Fixed.FromRatio(3, 2), Fixed.FromInt(3) / Fixed.FromInt(2));
        Assert.Equal(Fixed.FromInt(2), Fixed.FromInt(6) / Fixed.FromInt(3));
        Assert.Equal(Fixed.FromInt(-2), Fixed.FromInt(6) / Fixed.FromInt(-3));
    }

    [Fact]
    public void Division_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Fixed.FromInt(1) / Fixed.Zero);
    }

    [Fact]
    public void EqualityOperators_Work()
    {
        Assert.True(Fixed.FromInt(3) == Fixed.FromInt(3));
        Assert.False(Fixed.FromInt(3) == Fixed.FromInt(4));
        Assert.True(Fixed.FromInt(3) != Fixed.FromInt(4));
        Assert.False(Fixed.FromInt(3) != Fixed.FromInt(3));
    }

    [Fact]
    public void ComparisonOperators_Work()
    {
        Fixed two = Fixed.FromInt(2);
        Fixed three = Fixed.FromInt(3);
        Fixed alsoThree = Fixed.FromInt(3);

        Assert.True(two < three);
        Assert.False(three < two);
        Assert.True(three > two);
        Assert.False(two > three);

        Assert.True(three <= alsoThree);
        Assert.True(two <= three);
        Assert.False(three <= two);

        Assert.True(three >= alsoThree);
        Assert.True(three >= two);
        Assert.False(two >= three);
    }

    [Fact]
    public void Equals_AndHashCode_Consistent()
    {
        Fixed a = Fixed.FromInt(5);
        Fixed b = Fixed.FromInt(5);
        Fixed c = Fixed.FromInt(6);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals("not a Fixed"));
        Assert.False(a.Equals(null));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_ReturnsExpectedSign()
    {
        Fixed two = Fixed.FromInt(2);
        Fixed three = Fixed.FromInt(3);
        Fixed alsoThree = Fixed.FromInt(3);

        Assert.True(two.CompareTo(three) < 0);
        Assert.True(three.CompareTo(two) > 0);
        Assert.Equal(0, three.CompareTo(alsoThree));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2000L, 2)]
    [InlineData(-2000L, -2)]
    [InlineData(3500L, 3)]
    [InlineData(-3500L, -3)]
    [InlineData(999L, 0)]
    [InlineData(-999L, 0)]
    public void ToIntTruncated_TruncatesTowardZero(long raw, int expected)
    {
        Assert.Equal(expected, Fixed.FromRaw(raw).ToIntTruncated());
    }

    [Fact]
    public void ToDouble_ReturnsExpectedDebugValue()
    {
        Assert.Equal(0.0, Fixed.Zero.ToDouble());
        Assert.Equal(1.0, Fixed.One.ToDouble());
        Assert.Equal(1.5, Fixed.FromRatio(3, 2).ToDouble());
        Assert.Equal(-0.5, Fixed.FromRatio(-1, 2).ToDouble());
    }
}
