using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedRotationInverseLookupTests
{
    private static FixedVec2 Vec(int x, int y) => FixedVec2.FromInts(x, y);

    [Fact]
    public void StepCount_ReturnsCommittedGranularity()
    {
        Assert.Equal(1000, FixedRotationInverseLookup.StepCount);
    }

    [Fact]
    public void RotationFromIndex_zero_returns_zero_rotation()
    {
        Assert.Equal(Fixed.Zero, FixedRotationInverseLookup.RotationFromIndex(0));
    }

    [Fact]
    public void RotationFromIndex_quarter_returns_quarter_rotation()
    {
        Assert.Equal(Fixed.FromRatio(1, 4), FixedRotationInverseLookup.RotationFromIndex(250));
    }

    [Fact]
    public void RotationFromIndex_half_returns_half_rotation()
    {
        Assert.Equal(Fixed.FromRatio(1, 2), FixedRotationInverseLookup.RotationFromIndex(500));
    }

    [Fact]
    public void RotationFromIndex_threeQuarter_returns_three_quarter_rotation()
    {
        Assert.Equal(Fixed.FromRatio(3, 4), FixedRotationInverseLookup.RotationFromIndex(750));
    }

    [Fact]
    public void RotationFromIndex_negative_index_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRotationInverseLookup.RotationFromIndex(-1));
    }

    [Fact]
    public void RotationFromIndex_index_equals_StepCount_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FixedRotationInverseLookup.RotationFromIndex(FixedRotationInverseLookup.StepCount));
    }

    [Fact]
    public void FindNearestIndex_ZeroVector_ReturnsNull()
    {
        Assert.Null(FixedRotationInverseLookup.FindNearestIndex(FixedVec2.Zero));
    }

    [Fact]
    public void ResolveFromDirection_ZeroVector_ReturnsUnresolved()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(FixedVec2.Zero);

        Assert.False(resolution.IsResolved);
    }

    [Fact]
    public void ResolveFromDirection_PositiveX_ReturnsZeroRotation()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(10, 0));

        Assert.True(resolution.IsResolved);
        Assert.Equal(Fixed.Zero, resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_PositiveY_ReturnsQuarterRotation()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(0, 10));

        Assert.True(resolution.IsResolved);
        Assert.Equal(Fixed.FromRatio(1, 4), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_NegativeX_ReturnsHalfRotation()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(-10, 0));

        Assert.True(resolution.IsResolved);
        Assert.Equal(Fixed.FromRatio(1, 2), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_NegativeY_ReturnsThreeQuarterRotation()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(0, -10));

        Assert.True(resolution.IsResolved);
        Assert.Equal(Fixed.FromRatio(3, 4), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_PositiveDiagonal_ReturnsNearestLookupIndex()
    {
        const int expectedIndex = 124;

        Assert.Equal(expectedIndex, FixedRotationInverseLookup.FindNearestIndex(Vec(1, 1)));

        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(1, 1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(FixedRotationInverseLookup.RotationFromIndex(expectedIndex), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_NegativeXPositiveYDiagonal_ReturnsNearestLookupIndex()
    {
        const int expectedIndex = 374;

        Assert.Equal(expectedIndex, FixedRotationInverseLookup.FindNearestIndex(Vec(-1, 1)));

        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(-1, 1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(FixedRotationInverseLookup.RotationFromIndex(expectedIndex), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_NegativeDiagonal_ReturnsNearestLookupIndex()
    {
        const int expectedIndex = 624;

        Assert.Equal(expectedIndex, FixedRotationInverseLookup.FindNearestIndex(Vec(-1, -1)));

        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(-1, -1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(FixedRotationInverseLookup.RotationFromIndex(expectedIndex), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_PositiveXNegativeYDiagonal_ReturnsNearestLookupIndex()
    {
        const int expectedIndex = 874;

        Assert.Equal(expectedIndex, FixedRotationInverseLookup.FindNearestIndex(Vec(1, -1)));

        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(1, -1));

        Assert.True(resolution.IsResolved);
        Assert.Equal(FixedRotationInverseLookup.RotationFromIndex(expectedIndex), resolution.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_GeneralDirection_ReturnsResolvedRotationBetweenZeroAndQuarter()
    {
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(2, 1));

        Assert.True(resolution.IsResolved);
        Assert.True(resolution.Rotation > Fixed.Zero);
        Assert.True(resolution.Rotation < Fixed.FromRatio(1, 4));
    }

    [Fact]
    public void ResolveFromDirection_ScaledEquivalentDirections_ReturnSameRotation()
    {
        FixedRotationAimResolution small =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(2, 1));
        FixedRotationAimResolution large =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(4, 2));

        Assert.True(small.IsResolved);
        Assert.True(large.IsResolved);
        Assert.Equal(small.Rotation, large.Rotation);
    }

    [Fact]
    public void ResolveFromDirection_RepeatedCalls_AreDeterministic()
    {
        FixedVec2 direction = Vec(2, 1);

        FixedRotationAimResolution first =
            FixedRotationInverseLookup.ResolveFromDirection(direction);
        FixedRotationAimResolution second =
            FixedRotationInverseLookup.ResolveFromDirection(direction);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ResolveFromDirection_DiagonalRotationForwardPointsToSameQuadrant()
    {
        FixedRotationAimResolution aim =
            FixedRotationInverseLookup.ResolveFromDirection(Vec(1, 1));

        Assert.True(aim.IsResolved);
        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(aim.Rotation);

        Assert.True(forwardResult.IsResolved);
        Assert.NotNull(forwardResult.Forward);
        Assert.True(forwardResult.Forward.Value.X.Raw > 0);
        Assert.True(forwardResult.Forward.Value.Y.Raw > 0);
    }

    [Fact]
    public void ResolveFromDirection_NonCardinalRotationForwardHasPositiveDotProduct()
    {
        FixedVec2 dir = Vec(2, 1);
        FixedRotationAimResolution aim =
            FixedRotationInverseLookup.ResolveFromDirection(dir);
        FixedVec2 fwd =
            FixedRotationDirectionResolver.ResolveForward(aim.Rotation).Forward!.Value;

        Assert.True(dir.X * fwd.X + dir.Y * fwd.Y > Fixed.Zero);
    }

    [Fact]
    public void FindNearestIndex_RepeatedAmbiguousLikeInput_IsDeterministic()
    {
        FixedVec2 direction = Vec(1, 2);

        int? first = FixedRotationInverseLookup.FindNearestIndex(direction);
        int? second = FixedRotationInverseLookup.FindNearestIndex(direction);

        Assert.NotNull(first);
        Assert.Equal(first, second);
    }
}
