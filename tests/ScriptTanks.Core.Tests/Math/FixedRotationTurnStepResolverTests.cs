using System;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedRotationTurnStepResolverTests
{
    private static Fixed R(int raw) => Fixed.FromRaw(raw);

    private static Fixed Q(int numerator, int denominator) => Fixed.FromRatio(numerator, denominator);

    [Fact]
    public void Result_Constructor_PreservesValues()
    {
        var result = new FixedRotationTurnStepResult(
            R(10),
            R(20),
            R(30),
            R(5),
            R(5),
            isAligned: false);

        Assert.Equal(R(10), result.CurrentRotation);
        Assert.Equal(R(20), result.DesiredRotation);
        Assert.Equal(R(30), result.FinalRotation);
        Assert.Equal(R(5), result.AppliedTurnDelta);
        Assert.Equal(R(5), result.AppliedTurnMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Result_Constructor_NegativeAppliedTurnMagnitude_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedRotationTurnStepResult(
                R(0),
                R(1),
                R(1),
                R(1),
                R(-1),
                isAligned: true));

        Assert.Equal("appliedTurnMagnitude", exception.ParamName);
    }

    [Fact]
    public void ResolveStep_NegativeMaxTurnPerTick_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            FixedRotationTurnStepResolver.ResolveStep(R(0), R(1), R(-1)));

        Assert.Equal("maxTurnPerTick", exception.ParamName);
    }

    [Fact]
    public void ResolveStep_NormalizesCurrentRotation()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(1001),
            R(1),
            Q(1, 10));

        Assert.Equal(R(1), result.CurrentRotation);
        Assert.Equal(R(1), result.DesiredRotation);
        Assert.Equal(R(1), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(0, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_NormalizesDesiredRotation()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(1000),
            Q(1, 10));

        Assert.Equal(R(0), result.CurrentRotation);
        Assert.Equal(R(0), result.DesiredRotation);
        Assert.Equal(R(0), result.FinalRotation);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void ResolveStep_NormalizesNegativeRotation()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(-1),
            R(999),
            Q(1, 10));

        Assert.Equal(R(999), result.CurrentRotation);
        Assert.Equal(R(999), result.DesiredRotation);
        Assert.Equal(R(999), result.FinalRotation);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void ResolveStep_AlreadyAligned_ReturnsDesiredAndAligned()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(250),
            R(250),
            Q(1, 10));

        Assert.Equal(R(250), result.FinalRotation);
        Assert.Equal(R(250), result.DesiredRotation);
        Assert.Equal(0, result.AppliedTurnDelta.Raw);
        Assert.Equal(0, result.AppliedTurnMagnitude.Raw);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void ResolveStep_ZeroTurnRate_WhenNotAligned_DoesNotMove()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(250),
            Fixed.Zero);

        Assert.Equal(R(0), result.FinalRotation);
        Assert.Equal(R(0), result.CurrentRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(0, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_ZeroTurnRate_WhenAlreadyAligned_RemainsAligned()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(250),
            R(250),
            Fixed.Zero);

        Assert.Equal(R(250), result.FinalRotation);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void ResolveStep_DeltaWithinRate_SnapsToDesired()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(50),
            R(100));

        Assert.Equal(R(50), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(50, result.AppliedTurnDelta.Raw);
        Assert.Equal(50, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_DeltaEqualRate_SnapsToDesired()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(100),
            R(100));

        Assert.Equal(R(100), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(100, result.AppliedTurnDelta.Raw);
        Assert.Equal(100, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_PositiveDeltaBeyondRate_ClampsCounterClockwise()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(250),
            R(100));

        Assert.Equal(R(100), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(100, result.AppliedTurnDelta.Raw);
        Assert.Equal(100, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_NegativeDeltaBeyondRate_ClampsClockwise()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(250),
            R(0),
            R(100));

        Assert.Equal(R(150), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(-100, result.AppliedTurnDelta.Raw);
        Assert.Equal(100, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_WrapAroundPositiveDelta_UsesShortestCounterClockwisePath()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(950),
            R(50),
            R(40));

        Assert.Equal(R(990), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(40, result.AppliedTurnDelta.Raw);
        Assert.Equal(40, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_WrapAroundPositiveDelta_SnapsWhenRateEnough()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(950),
            R(50),
            R(100));

        Assert.Equal(R(50), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(100, result.AppliedTurnDelta.Raw);
        Assert.NotEqual(-900, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_WrapAroundNegativeDelta_UsesShortestClockwisePath()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(50),
            R(950),
            R(40));

        Assert.Equal(R(10), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(-40, result.AppliedTurnDelta.Raw);
        Assert.Equal(40, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_WrapAroundNegativeDelta_NormalizesBelowZero()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(20),
            R(900),
            R(50));

        Assert.Equal(R(970), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(-50, result.AppliedTurnDelta.Raw);
        Assert.Equal(50, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_ExactHalfTurnTie_ChoosesCounterClockwise()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(500),
            R(100));

        Assert.Equal(R(100), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(100, result.AppliedTurnDelta.Raw);
        Assert.Equal(100, result.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_ExactHalfTurnTie_SnapsCounterClockwiseWhenRateEnough()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(500),
            R(500));

        Assert.Equal(R(500), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(500, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_ReverseHalfTurnTie_AlsoChoosesCounterClockwise()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(500),
            R(0),
            R(100));

        Assert.Equal(R(600), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(100, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_AppliedTurnMagnitude_IsAbsoluteDelta()
    {
        FixedRotationTurnStepResult positive = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            R(250),
            R(100));

        Assert.Equal(System.Math.Abs(positive.AppliedTurnDelta.Raw), positive.AppliedTurnMagnitude.Raw);

        FixedRotationTurnStepResult negative = FixedRotationTurnStepResolver.ResolveStep(
            R(250),
            R(0),
            R(100));

        Assert.Equal(System.Math.Abs(negative.AppliedTurnDelta.Raw), negative.AppliedTurnMagnitude.Raw);
    }

    [Fact]
    public void ResolveStep_RepeatedCalls_ReturnSameResult()
    {
        Fixed current = R(120);
        Fixed desired = R(840);
        Fixed rate = Q(3, 20);

        FixedRotationTurnStepResult first =
            FixedRotationTurnStepResolver.ResolveStep(current, desired, rate);
        FixedRotationTurnStepResult second =
            FixedRotationTurnStepResolver.ResolveStep(current, desired, rate);

        Assert.Equal(first.FinalRotation, second.FinalRotation);
        Assert.Equal(first.AppliedTurnDelta, second.AppliedTurnDelta);
        Assert.Equal(first.AppliedTurnMagnitude, second.AppliedTurnMagnitude);
        Assert.Equal(first.IsAligned, second.IsAligned);
    }

    [Fact]
    public void ResolveStep_FromFullAngleAimRotation_ClampsTowardInverseLookupRotation()
    {
        FixedRotationAimResolution aim = FixedRotationInverseLookup.ResolveFromDirection(
            FixedVec2.FromInts(10, 10));

        Assert.True(aim.IsResolved);

        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(0),
            aim.Rotation,
            R(50));

        Assert.Equal(R(50), result.FinalRotation);
        Assert.False(result.IsAligned);
        Assert.Equal(50, result.AppliedTurnDelta.Raw);
    }

    [Fact]
    public void ResolveStep_MaxTurnAtLeastFullTurn_SnapsToDesired()
    {
        FixedRotationTurnStepResult result = FixedRotationTurnStepResolver.ResolveStep(
            R(100),
            R(700),
            Fixed.One);

        Assert.Equal(R(700), result.FinalRotation);
        Assert.True(result.IsAligned);
        Assert.Equal(-400, result.AppliedTurnDelta.Raw);
        Assert.Equal(400, result.AppliedTurnMagnitude.Raw);
        Assert.NotEqual(R(1100).Raw, result.FinalRotation.Raw);
    }
}
