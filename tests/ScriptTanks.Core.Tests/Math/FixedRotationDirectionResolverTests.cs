using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedRotationDirectionResolverTests
{
    private static void AssertResolvedForward(Fixed rotation, FixedVec2 expected)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.Equal(FixedRotationDirectionStatus.Resolved, result.Status);
        Assert.True(result.IsResolved);
        Assert.NotNull(result.Forward);
        Assert.Equal(expected, result.Forward.Value);
    }

    private static FixedVec2 Forward(Fixed rotation) =>
        FixedRotationDirectionResolver.ResolveForward(rotation).Forward!.Value;

    [Fact]
    public void ResolveForward_Zero_returns_east()
    {
        AssertResolvedForward(Fixed.Zero, FixedVec2.FromInts(1, 0));
    }

    [Fact]
    public void ResolveForward_quarter_turn_returns_north()
    {
        AssertResolvedForward(Fixed.FromRatio(1, 4), FixedVec2.FromInts(0, 1));
    }

    [Fact]
    public void ResolveForward_half_turn_returns_west()
    {
        AssertResolvedForward(Fixed.FromRatio(1, 2), FixedVec2.FromInts(-1, 0));
    }

    [Fact]
    public void ResolveForward_three_quarter_turn_returns_south()
    {
        AssertResolvedForward(Fixed.FromRatio(3, 4), FixedVec2.FromInts(0, -1));
    }

    [Fact]
    public void ResolveForward_one_full_turn_wraps_to_east()
    {
        AssertResolvedForward(Fixed.One, FixedVec2.FromInts(1, 0));
    }

    [Fact]
    public void ResolveForward_positive_wrap_matches_quarter_turn()
    {
        FixedVec2 quarter = Forward(Fixed.FromRatio(1, 4));
        Assert.Equal(quarter, Forward(Fixed.FromRatio(5, 4)));
        Assert.Equal(quarter, Forward(Fixed.FromRaw(10_000 + 250)));
    }

    [Fact]
    public void ResolveForward_negative_raw_wraps_to_three_quarter_turn()
    {
        FixedVec2 threeQuarter = Forward(Fixed.FromRatio(3, 4));
        Assert.Equal(threeQuarter, Forward(Fixed.FromRaw(-250)));
        Assert.Equal(threeQuarter, Forward(Fixed.FromRaw(-3_250)));
    }

    [Fact]
    public void ResolveForward_eighth_turn_returns_resolved_diagonal()
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(Fixed.FromRatio(1, 8));

        Assert.True(result.IsResolved);
        Assert.NotNull(result.Forward);
        Assert.True(result.Forward.Value.X.Raw > 0);
        Assert.True(result.Forward.Value.Y.Raw > 0);
    }

    [Fact]
    public void ResolveForward_eighth_turn_is_not_a_cardinal()
    {
        FixedVec2 diagonal = Forward(Fixed.FromRatio(1, 8));

        Assert.NotEqual(FixedVec2.FromInts(1, 0), diagonal);
        Assert.NotEqual(FixedVec2.FromInts(0, 1), diagonal);
        Assert.NotEqual(FixedVec2.FromInts(-1, 0), diagonal);
        Assert.NotEqual(FixedVec2.FromInts(0, -1), diagonal);
    }

    [Fact]
    public void ResolveForward_eighth_turn_matches_committed_table_constants()
    {
        FixedVec2 diagonal = Forward(Fixed.FromRatio(1, 8));
        Fixed ratio707 = Fixed.FromRatio(707, 1000);

        Assert.Equal(ratio707, diagonal.X);
        Assert.Equal(ratio707, diagonal.Y);
    }

    [Fact]
    public void ResolveForward_five_eighth_turn_has_negative_components()
    {
        FixedVec2 diagonal = Forward(Fixed.FromRatio(5, 8));

        Assert.True(diagonal.X.Raw < 0);
        Assert.True(diagonal.Y.Raw < 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(750)]
    [InlineData(125)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(-250)]
    [InlineData(-3250)]
    public void ResolveForward_normal_inputs_never_return_UnsupportedRotationConvention(
        long raw)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(Fixed.FromRaw(raw));

        Assert.NotEqual(
            FixedRotationDirectionStatus.UnsupportedRotationConvention,
            result.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(750)]
    [InlineData(125)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(-250)]
    public void ResolveForward_normal_inputs_never_return_InvalidRotationValue(long raw)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(Fixed.FromRaw(raw));

        Assert.NotEqual(FixedRotationDirectionStatus.InvalidRotationValue, result.Status);
    }

    [Fact]
    public void ResolveForward_repeated_calls_are_deterministic()
    {
        Fixed rotation = Fixed.FromRatio(1, 8);

        FixedRotationDirectionResult first =
            FixedRotationDirectionResolver.ResolveForward(rotation);
        FixedRotationDirectionResult second =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Forward, second.Forward);
    }

    [Fact]
    public void ResolveForward_raw_999_is_resolved()
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(Fixed.FromRaw(999));

        Assert.True(result.IsResolved);
        Assert.NotNull(result.Forward);
    }

    [Fact]
    public void ResolveForward_raw_1000_equals_raw_0()
    {
        Assert.Equal(
            Forward(Fixed.Zero),
            Forward(Fixed.FromRaw(1000)));
    }
}
