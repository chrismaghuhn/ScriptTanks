using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptConditionEvaluatorTests
{
    private static TankId Tank(int value) => new(value);

    private static Fixed R(int raw) => Fixed.FromRaw(raw);

    private static ScriptEvaluationContext CreateContext(
        bool enemyVisible = true,
        bool weaponReady = true,
        int myHitPoints = 50,
        Fixed? enemyDistance = null,
        bool sensorReady = true,
        ScriptVisibleTurretAimStatusResult? turretAimStatus = null)
    {
        return new ScriptEvaluationContext(
            enemyVisible,
            weaponReady,
            myHitPoints,
            enemyDistance ?? Fixed.FromInt(10),
            sensorReady,
            turretAimStatus ?? ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptVisibleTurretAimStatusResult AlignedAimStatus() =>
        ScriptVisibleTurretAimStatusResult.Aligned(
            Tank(0), 0, Tank(1), 1, Fixed.Zero, Fixed.Zero);

    private static ScriptVisibleTurretAimStatusResult TurningAimStatus() =>
        ScriptVisibleTurretAimStatusResult.Turning(
            Tank(0), 0, Tank(1), 1, Fixed.Zero, Fixed.Zero, R(250), R(250));

    private static ScriptVisibleTurretAimStatusResult MissingAimSolutionStatus() =>
        ScriptVisibleTurretAimStatusResult.MissingAimSolution(
            Tank(0), 0, Tank(1), 1, Fixed.Zero);

    private static ScriptVisibleTurretAimStatusResult NoTargetAimStatus() =>
        ScriptVisibleTurretAimStatusResult.NoTarget(Tank(0), 0, Fixed.Zero);

    private static ScriptVisibleTurretAimStatusResult OwnerDestroyedAimStatus() =>
        ScriptVisibleTurretAimStatusResult.OwnerDestroyed(Tank(0), 0, Fixed.Zero);

    private static ScriptCondition CreateCondition(
        ScriptConditionType type,
        string argument = "")
    {
        return new ScriptCondition(type, argument);
    }

    [Fact]
    public void Always_ReturnsTrue()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.Always, "ignored"),
            CreateContext());

        Assert.True(result);
    }

    [Fact]
    public void EnemyVisible_ReturnsTrue_WhenContextEnemyVisibleTrue()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyVisible, string.Empty),
            CreateContext(enemyVisible: true));

        Assert.True(result);
    }

    [Fact]
    public void EnemyVisible_ReturnsFalse_WhenContextEnemyVisibleFalse()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyVisible, string.Empty),
            CreateContext(enemyVisible: false));

        Assert.False(result);
    }

    [Fact]
    public void WeaponReady_ReturnsTrue_WhenContextWeaponReadyTrue()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.WeaponReady, string.Empty),
            CreateContext(weaponReady: true));

        Assert.True(result);
    }

    [Fact]
    public void WeaponReady_ReturnsFalse_WhenContextWeaponReadyFalse()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.WeaponReady, string.Empty),
            CreateContext(weaponReady: false));

        Assert.False(result);
    }

    [Fact]
    public void SensorReady_ReturnsTrue_WhenContextSensorReadyTrue()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.SensorReady, string.Empty),
            CreateContext(sensorReady: true));

        Assert.True(result);
    }

    [Fact]
    public void SensorReady_ReturnsFalse_WhenContextSensorReadyFalse()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.SensorReady, string.Empty),
            CreateContext(sensorReady: false));

        Assert.False(result);
    }

    [Fact]
    public void MyHpBelow_ReturnsTrue_WhenHitPointsBelowThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MyHpBelow, "60"),
            CreateContext(myHitPoints: 50));

        Assert.True(result);
    }

    [Fact]
    public void MyHpBelow_ReturnsFalse_WhenHitPointsEqualThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MyHpBelow, "50"),
            CreateContext(myHitPoints: 50));

        Assert.False(result);
    }

    [Fact]
    public void MyHpBelow_ReturnsFalse_WhenHitPointsAboveThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MyHpBelow, "40"),
            CreateContext(myHitPoints: 50));

        Assert.False(result);
    }

    [Fact]
    public void MyHpBelow_AllowsThresholdZero()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MyHpBelow, "0"),
            CreateContext(myHitPoints: 0));

        Assert.False(result);
    }

    [Fact]
    public void MyHpBelow_RejectsEmptyArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, string.Empty),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void MyHpBelow_RejectsWhitespaceArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "   "),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void MyHpBelow_RejectsNonNumericArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "abc"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void MyHpBelow_RejectsNegativeArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "-5"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void MyHpBelow_RejectsDecimalArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "3.14"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void EnemyDistanceBelow_ReturnsTrue_WhenDistanceBelowThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyDistanceBelow, "20"),
            CreateContext(enemyDistance: Fixed.FromInt(5)));

        Assert.True(result);
    }

    [Fact]
    public void EnemyDistanceBelow_ReturnsFalse_WhenDistanceEqualsThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyDistanceBelow, "10"),
            CreateContext(enemyDistance: Fixed.FromInt(10)));

        Assert.False(result);
    }

    [Fact]
    public void EnemyDistanceBelow_ReturnsFalse_WhenDistanceAboveThreshold()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyDistanceBelow, "10"),
            CreateContext(enemyDistance: Fixed.FromInt(15)));

        Assert.False(result);
    }

    [Fact]
    public void EnemyDistanceBelow_AllowsThresholdZero()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyDistanceBelow, "0"),
            CreateContext(enemyDistance: Fixed.Zero));

        Assert.False(result);
    }

    [Fact]
    public void EnemyDistanceBelow_RejectsInvalidArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.EnemyDistanceBelow, "not-int"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void NumericThreshold_RejectsCommaDecimalLikeOneCommaFive_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "1,5"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void BooleanConditions_IgnoreArguments()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.EnemyVisible, "not-a-valid-threshold"),
            CreateContext(enemyVisible: true));

        Assert.True(result);
    }

    [Fact]
    public void MyHpBelow_RejectsPlusSignPrefixedArgument_WithParamNameCondition()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ScriptConditionEvaluator.Evaluate(
                CreateCondition(ScriptConditionType.MyHpBelow, "+5"),
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    [Fact]
    public void TurretAligned_ReturnsTrue_WhenStatusAligned()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretAligned, string.Empty),
            CreateContext(turretAimStatus: AlignedAimStatus()));

        Assert.True(result);
    }

    [Fact]
    public void TurretAligned_ReturnsFalse_WhenStatusTurning()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretAligned, string.Empty),
            CreateContext(turretAimStatus: TurningAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void TurretAligned_ReturnsFalse_WhenStatusNoTarget()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretAligned, string.Empty),
            CreateContext(turretAimStatus: NoTargetAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void TurretAligned_ReturnsFalse_WhenStatusOwnerDestroyed()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretAligned, string.Empty),
            CreateContext(turretAimStatus: OwnerDestroyedAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void TurretTurning_ReturnsTrue_WhenStatusTurning()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretTurning, string.Empty),
            CreateContext(turretAimStatus: TurningAimStatus()));

        Assert.True(result);
    }

    [Fact]
    public void TurretTurning_ReturnsFalse_WhenStatusAligned()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretTurning, string.Empty),
            CreateContext(turretAimStatus: AlignedAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void TurretTurning_ReturnsFalse_WhenStatusMissingAimSolution()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.TurretTurning, string.Empty),
            CreateContext(turretAimStatus: MissingAimSolutionStatus()));

        Assert.False(result);
    }

    [Fact]
    public void HasAimTarget_ReturnsTrue_WhenStatusAligned()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.HasAimTarget, string.Empty),
            CreateContext(turretAimStatus: AlignedAimStatus()));

        Assert.True(result);
    }

    [Fact]
    public void HasAimTarget_ReturnsTrue_WhenStatusTurning()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.HasAimTarget, string.Empty),
            CreateContext(turretAimStatus: TurningAimStatus()));

        Assert.True(result);
    }

    [Fact]
    public void HasAimTarget_ReturnsTrue_WhenStatusMissingAimSolution()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.HasAimTarget, string.Empty),
            CreateContext(turretAimStatus: MissingAimSolutionStatus()));

        Assert.True(result);
    }

    [Fact]
    public void HasAimTarget_ReturnsFalse_WhenStatusNoTarget()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.HasAimTarget, string.Empty),
            CreateContext(turretAimStatus: NoTargetAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void HasAimTarget_ReturnsFalse_WhenStatusOwnerDestroyed()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.HasAimTarget, string.Empty),
            CreateContext(turretAimStatus: OwnerDestroyedAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void MissingAimSolution_ReturnsTrue_WhenStatusMissingAimSolution()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MissingAimSolution, string.Empty),
            CreateContext(turretAimStatus: MissingAimSolutionStatus()));

        Assert.True(result);
    }

    [Fact]
    public void MissingAimSolution_ReturnsFalse_WhenStatusAligned()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MissingAimSolution, string.Empty),
            CreateContext(turretAimStatus: AlignedAimStatus()));

        Assert.False(result);
    }

    [Fact]
    public void MissingAimSolution_ReturnsFalse_WhenStatusNoTarget()
    {
        bool result = ScriptConditionEvaluator.Evaluate(
            CreateCondition(ScriptConditionType.MissingAimSolution, string.Empty),
            CreateContext(turretAimStatus: NoTargetAimStatus()));

        Assert.False(result);
    }
}
