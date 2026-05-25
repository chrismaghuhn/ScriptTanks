using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptVisibleTurretAimStatusResultTests
{
    private static TankId Tank(int value) => new(value);

    private static Fixed R(int raw) => Fixed.FromRaw(raw);

    [Fact]
    public void ScriptVisibleTurretAimStatus_HasStableValues()
    {
        Assert.Equal(0, (int)ScriptVisibleTurretAimStatus.NoTarget);
        Assert.Equal(1, (int)ScriptVisibleTurretAimStatus.MissingAimSolution);
        Assert.Equal(2, (int)ScriptVisibleTurretAimStatus.Aligned);
        Assert.Equal(3, (int)ScriptVisibleTurretAimStatus.Turning);
        Assert.Equal(4, (int)ScriptVisibleTurretAimStatus.OwnerDestroyed);
    }

    [Fact]
    public void ScriptVisibleTurretAimStatus_HasExpectedMemberCount()
    {
        string[] names = Enum.GetNames<ScriptVisibleTurretAimStatus>();
        Assert.Equal(5, names.Length);
    }

    [Fact]
    public void NoTarget_PreservesOwnerAndCurrentRotation()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.NoTarget(
            Tank(3),
            tankIndex: 2,
            R(100));

        Assert.Equal(ScriptVisibleTurretAimStatus.NoTarget, result.Status);
        Assert.Equal(Tank(3), result.TankId);
        Assert.Equal(2, result.TankIndex);
        Assert.Equal(R(100), result.CurrentRotation);
    }

    [Fact]
    public void OwnerDestroyed_PreservesOwnerAndCurrentRotation()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.OwnerDestroyed(
            Tank(5),
            tankIndex: 1,
            R(250));

        Assert.Equal(ScriptVisibleTurretAimStatus.OwnerDestroyed, result.Status);
        Assert.Equal(Tank(5), result.TankId);
        Assert.Equal(1, result.TankIndex);
        Assert.Equal(R(250), result.CurrentRotation);
    }

    [Fact]
    public void MissingAimSolution_PreservesTargetAndCurrentRotation()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.MissingAimSolution(
            Tank(0),
            tankIndex: 0,
            Tank(1),
            targetTankIndex: 1,
            R(50));

        Assert.Equal(ScriptVisibleTurretAimStatus.MissingAimSolution, result.Status);
        Assert.Equal(Tank(1), result.TargetTankId);
        Assert.Equal(1, result.TargetTankIndex);
        Assert.Equal(R(50), result.CurrentRotation);
    }

    [Fact]
    public void Aligned_PreservesTargetDesiredRotationAndSetsAligned()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Aligned(
            Tank(0),
            tankIndex: 0,
            Tank(2),
            targetTankIndex: 2,
            R(100),
            R(100));

        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, result.Status);
        Assert.Equal(Tank(2), result.TargetTankId);
        Assert.Equal(R(100), result.DesiredRotation);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void Turning_PreservesTargetDesiredDeltaAndMagnitude()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0),
            tankIndex: 0,
            Tank(1),
            targetTankIndex: 1,
            R(10),
            R(200),
            R(-50),
            R(50));

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(Tank(1), result.TargetTankId);
        Assert.Equal(R(200), result.DesiredRotation);
        Assert.Equal(R(-50), result.AlignmentDelta);
        Assert.Equal(R(50), result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void NoTarget_HasNoTargetOrDesiredFields()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.NoTarget(
            Tank(0),
            tankIndex: 0,
            R(0));

        Assert.Null(result.TargetTankId);
        Assert.Null(result.TargetTankIndex);
        Assert.Null(result.DesiredRotation);
        Assert.Null(result.AlignmentDelta);
        Assert.Null(result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
        Assert.False(result.HasTarget);
    }

    [Fact]
    public void OwnerDestroyed_HasNoTargetOrDesiredFields()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.OwnerDestroyed(
            Tank(0),
            tankIndex: 0,
            R(0));

        Assert.Null(result.TargetTankId);
        Assert.Null(result.TargetTankIndex);
        Assert.Null(result.DesiredRotation);
        Assert.Null(result.AlignmentDelta);
        Assert.Null(result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
        Assert.False(result.HasTarget);
    }

    [Fact]
    public void MissingAimSolution_HasTargetButNoDesiredOrDelta()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.MissingAimSolution(
            Tank(0),
            tankIndex: 0,
            Tank(1),
            targetTankIndex: 1,
            R(0));

        Assert.True(result.HasTarget);
        Assert.Null(result.DesiredRotation);
        Assert.Null(result.AlignmentDelta);
        Assert.Null(result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Aligned_HasZeroDeltaMagnitudeAndIsAligned()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Aligned(
            Tank(0),
            tankIndex: 0,
            Tank(1),
            targetTankIndex: 1,
            R(300),
            R(300));

        Assert.Equal(Fixed.Zero, result.AlignmentDelta);
        Assert.Equal(Fixed.Zero, result.AlignmentErrorMagnitude);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void Aligned_HasZeroDeltaAndZeroMagnitude()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Aligned(
            Tank(0),
            0,
            Tank(1),
            1,
            R(0),
            R(0));

        Assert.NotNull(result.AlignmentDelta);
        Assert.NotNull(result.AlignmentErrorMagnitude);
        Assert.Equal(Fixed.Zero, result.AlignmentDelta);
        Assert.Equal(Fixed.Zero, result.AlignmentErrorMagnitude);
    }

    [Fact]
    public void Turning_HasPositiveErrorMagnitudeAndIsNotAligned()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0),
            0,
            Tank(1),
            1,
            R(0),
            R(100),
            R(100),
            R(100));

        Assert.True(result.AlignmentErrorMagnitude > Fixed.Zero);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Turning_AcceptsNegativeAlignmentDelta()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0),
            0,
            Tank(1),
            1,
            R(200),
            R(50),
            R(-75),
            R(75));

        Assert.Equal(R(-75), result.AlignmentDelta);
        Assert.Equal(R(75), result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Turning_WithPositiveDeltaAllowed()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0),
            0,
            Tank(1),
            1,
            R(0),
            R(50),
            R(25),
            R(25));

        Assert.True(result.AlignmentDelta > Fixed.Zero);
    }

    [Fact]
    public void Turning_WithNegativeDeltaAllowed()
    {
        ScriptVisibleTurretAimStatusResult result = ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0),
            0,
            Tank(1),
            1,
            R(100),
            R(0),
            R(-10),
            R(10));

        Assert.True(result.AlignmentDelta < Fixed.Zero);
    }

    [Fact]
    public void Constructor_InvalidStatus_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                (ScriptVisibleTurretAimStatus)999,
                Tank(0),
                0,
                null,
                null,
                R(0),
                null,
                null,
                null,
                isAligned: false));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeTankIndex_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.NoTarget,
                Tank(0),
                tankIndex: -1,
                null,
                null,
                R(0),
                null,
                null,
                null,
                isAligned: false));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeTargetTankIndex_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.MissingAimSolution,
                Tank(0),
                0,
                Tank(1),
                targetTankIndex: -1,
                R(0),
                null,
                null,
                null,
                isAligned: false));

        Assert.Equal("targetTankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeAlignmentErrorMagnitude_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Turning,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                R(10),
                R(-1),
                isAligned: false));

        Assert.Equal("alignmentErrorMagnitude", ex.ParamName);
    }

    [Fact]
    public void Constructor_NoTarget_WithTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.NoTarget,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                null,
                null,
                null,
                isAligned: false));
    }

    [Fact]
    public void Constructor_OwnerDestroyed_WithTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.OwnerDestroyed,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                null,
                null,
                null,
                isAligned: false));
    }

    [Fact]
    public void Constructor_MissingAimSolution_WithoutTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.MissingAimSolution,
                Tank(0),
                0,
                null,
                null,
                R(0),
                null,
                null,
                null,
                isAligned: false));
    }

    [Fact]
    public void Constructor_Aligned_WithoutDesiredRotation_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Aligned,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                null,
                Fixed.Zero,
                Fixed.Zero,
                isAligned: true));
    }

    [Fact]
    public void Constructor_Aligned_WithNonZeroDelta_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Aligned,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                R(10),
                Fixed.Zero,
                isAligned: true));
    }

    [Fact]
    public void Constructor_Aligned_WithNonZeroMagnitude_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Aligned,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                Fixed.Zero,
                R(5),
                isAligned: true));
    }

    [Fact]
    public void Constructor_Aligned_WithNullDelta_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Aligned,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                null,
                Fixed.Zero,
                isAligned: true));
    }

    [Fact]
    public void Constructor_Turning_WithoutDesiredRotation_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Turning,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                null,
                R(10),
                R(10),
                isAligned: false));
    }

    [Fact]
    public void Constructor_Turning_WithZeroMagnitude_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Turning,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                R(10),
                Fixed.Zero,
                isAligned: false));
    }

    [Fact]
    public void Constructor_Turning_WithIsAlignedTrue_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScriptVisibleTurretAimStatusResult(
                ScriptVisibleTurretAimStatus.Turning,
                Tank(0),
                0,
                Tank(1),
                1,
                R(0),
                R(100),
                R(10),
                R(10),
                isAligned: true));
    }

    [Fact]
    public void Results_UseReferenceEqualityByDefault()
    {
        ScriptVisibleTurretAimStatusResult a = ScriptVisibleTurretAimStatusResult.NoTarget(
            Tank(0),
            0,
            R(0));

        ScriptVisibleTurretAimStatusResult b = ScriptVisibleTurretAimStatusResult.NoTarget(
            Tank(0),
            0,
            R(0));

        Assert.NotSame(a, b);
        Assert.False(ReferenceEquals(a, b));
        Assert.NotEqual(a, b);
    }
}
