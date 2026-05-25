using ScriptTanks.Core.Math;

using Xunit;



namespace ScriptTanks.Core.Tests.Math;



public sealed class FixedRotationAimResolverTests

{

    private static FixedVec2 Vec(int x, int y) => FixedVec2.FromInts(x, y);



    [Fact]

    public void ResolveFromDirection_PositiveX_ReturnsZeroRotation()

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(FixedVec2.FromInts(10, 0));



        Assert.True(resolution.IsResolved);

        Assert.Equal(Fixed.Zero, resolution.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_PositiveY_ReturnsQuarterRotation()

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(FixedVec2.FromInts(0, 10));



        Assert.True(resolution.IsResolved);

        Assert.Equal(Fixed.FromRatio(1, 4), resolution.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_NegativeX_ReturnsHalfRotation()

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(FixedVec2.FromInts(-10, 0));



        Assert.True(resolution.IsResolved);

        Assert.Equal(Fixed.FromRatio(1, 2), resolution.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_NegativeY_ReturnsThreeQuarterRotation()

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(FixedVec2.FromInts(0, -10));



        Assert.True(resolution.IsResolved);

        Assert.Equal(Fixed.FromRatio(3, 4), resolution.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_ZeroVector_ReturnsUnresolved()

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(FixedVec2.Zero);



        Assert.False(resolution.IsResolved);

    }



    [Fact]

    public void ResolveFromDirection_DiagonalVector_ReturnsResolvedViaInverseLookup()

    {

        FixedVec2 direction = FixedVec2.FromInts(10, 10);



        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(direction);



        Assert.True(resolution.IsResolved);

        Assert.Equal(

            FixedRotationInverseLookup.ResolveFromDirection(direction).Rotation,

            resolution.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_NorthWestDiagonal_ReturnsInverseLookupRotation()

    {

        AssertInverseLookupDelegation(Vec(-1, 1));

    }



    [Fact]

    public void ResolveFromDirection_SouthWestDiagonal_ReturnsInverseLookupRotation()

    {

        AssertInverseLookupDelegation(Vec(-1, -1));

    }



    [Fact]

    public void ResolveFromDirection_SouthEastDiagonal_ReturnsInverseLookupRotation()

    {

        AssertInverseLookupDelegation(Vec(1, -1));

    }



    [Fact]

    public void ResolveFromDirection_NonCardinalVector_ReturnsResolved()

    {

        AssertInverseLookupDelegation(Vec(2, 1));

    }



    [Fact]

    public void ResolveFromDirection_ScaledEquivalentDirection_ReturnsSameRotation()

    {

        FixedRotationAimResolution small =

            FixedRotationAimResolver.ResolveFromDirection(Vec(2, 1));

        FixedRotationAimResolution large =

            FixedRotationAimResolver.ResolveFromDirection(Vec(4, 2));



        Assert.True(small.IsResolved);

        Assert.True(large.IsResolved);

        Assert.Equal(small.Rotation, large.Rotation);

    }



    [Fact]

    public void ResolveFromDirection_RepeatedCalls_ReturnSameRotation()

    {

        FixedVec2 direction = Vec(2, 1);



        FixedRotationAimResolution first =

            FixedRotationAimResolver.ResolveFromDirection(direction);

        FixedRotationAimResolution second =

            FixedRotationAimResolver.ResolveFromDirection(direction);



        Assert.Equal(first, second);

    }



    [Fact]

    public void Resolved_preserves_rotation()

    {

        Fixed expected = Fixed.FromRatio(1, 4);



        FixedRotationAimResolution resolution = FixedRotationAimResolution.Resolved(expected);



        Assert.True(resolution.IsResolved);

        Assert.Equal(expected, resolution.Rotation);

    }



    [Fact]

    public void Unresolved_has_no_meaningful_rotation()

    {

        FixedRotationAimResolution resolution = FixedRotationAimResolution.Unresolved();



        Assert.False(resolution.IsResolved);

        Assert.Equal(default, resolution.Rotation);

    }



    private static void AssertInverseLookupDelegation(FixedVec2 direction)

    {

        FixedRotationAimResolution resolution =

            FixedRotationAimResolver.ResolveFromDirection(direction);



        Assert.True(resolution.IsResolved);

        Assert.Equal(

            FixedRotationInverseLookup.ResolveFromDirection(direction).Rotation,

            resolution.Rotation);

    }

}


