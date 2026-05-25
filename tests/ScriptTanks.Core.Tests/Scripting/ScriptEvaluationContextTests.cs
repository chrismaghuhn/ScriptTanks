using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptEvaluationContextTests
{
    private static ScriptEvaluationContext CreateContext()
    {
        return new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    [Fact]
    public void Constructor_NullTurretAimStatus_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ScriptEvaluationContext(
                enemyVisible: true,
                weaponReady: true,
                myHitPoints: 75,
                enemyDistance: Fixed.Zero,
                sensorReady: true,
                turretAimStatus: null!));

        Assert.Equal("turretAimStatus", ex.ParamName);
    }

    [Fact]
    public void Constructor_PreservesTurretAimStatus()
    {
        ScriptVisibleTurretAimStatusResult aimStatus =
            ScriptVisibleTurretAimStatusResult.Aligned(
                new TankId(0),
                0,
                new TankId(1),
                1,
                Fixed.Zero,
                Fixed.Zero);

        var context = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: aimStatus);

        Assert.Same(aimStatus, context.TurretAimStatus);
        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, context.TurretAimStatus.Status);
    }

    [Fact]
    public void Equals_TreatsTurretAimStatusByStatus_NotResultReference()
    {
        ScriptVisibleTurretAimStatusResult aimStatusA =
            ScriptVisibleTurretAimStatusResult.Aligned(
                new TankId(0),
                0,
                new TankId(1),
                1,
                Fixed.Zero,
                Fixed.Zero);
        ScriptVisibleTurretAimStatusResult aimStatusB =
            ScriptVisibleTurretAimStatusResult.Aligned(
                new TankId(0),
                0,
                new TankId(2),
                2,
                Fixed.FromRaw(100),
                Fixed.FromRaw(100));

        var a = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: aimStatusA);
        var b = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: aimStatusB);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Constructor_PreservesBooleanFields()
    {
        var context = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: false,
            myHitPoints: 50,
            enemyDistance: Fixed.Zero,
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.True(context.EnemyVisible);
        Assert.False(context.WeaponReady);
        Assert.True(context.SensorReady);
    }

    [Fact]
    public void Constructor_PreservesMyHitPoints()
    {
        var context = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: false,
            myHitPoints: 99,
            enemyDistance: Fixed.Zero,
            sensorReady: false,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.Equal(99, context.MyHitPoints);
    }

    [Fact]
    public void Constructor_PreservesEnemyDistance()
    {
        Fixed distance = Fixed.FromInt(7);

        var context = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: false,
            myHitPoints: 10,
            enemyDistance: distance,
            sensorReady: false,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.Equal(distance, context.EnemyDistance);
    }

    [Fact]
    public void Constructor_RejectsNegativeMyHitPoints_WithParamNameMyHitPoints()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScriptEvaluationContext(
                enemyVisible: true,
                weaponReady: true,
                myHitPoints: -1,
                enemyDistance: Fixed.Zero,
                sensorReady: true,
                    turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus));

        Assert.Equal("myHitPoints", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNegativeEnemyDistance_WithParamNameEnemyDistance()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScriptEvaluationContext(
                enemyVisible: true,
                weaponReady: true,
                myHitPoints: 75,
                enemyDistance: Fixed.FromInt(-1),
                sensorReady: true,
                    turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus));

        Assert.Equal("enemyDistance", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsZeroMyHitPoints()
    {
        var context = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: false,
            myHitPoints: 0,
            enemyDistance: Fixed.Zero,
            sensorReady: false,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.Equal(0, context.MyHitPoints);
    }

    [Fact]
    public void Constructor_AllowsZeroEnemyDistance()
    {
        var context = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 5,
            enemyDistance: Fixed.Zero,
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.Equal(Fixed.Zero, context.EnemyDistance);
    }

    [Fact]
    public void Constructor_AllowsEnemyNotVisibleWithNonzeroEnemyDistance()
    {
        var context = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(25),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.False(context.EnemyVisible);
        Assert.Equal(Fixed.FromInt(25), context.EnemyDistance);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameValues()
    {
        var a = CreateContext();
        var b = CreateContext();

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentBooleanField()
    {
        var a = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
        var b = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentHitPoints()
    {
        var a = CreateContext();
        var b = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 10,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentEnemyDistance()
    {
        var a = CreateContext();
        var b = new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(99),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Operators_MatchEqualsSemantics()
    {
        var x = CreateContext();
        var y = CreateContext();
        var z = new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: true,
            myHitPoints: 75,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);

        Assert.True(x == y);
        Assert.False(x == z);
        Assert.False(x != y);
        Assert.True(x != z);
    }

    [Fact]
    public void ToString_IncludesAllFieldNamesAndValuesUsingInvariantCulture()
    {
        ScriptEvaluationContext context = CreateContext();

        string value = context.ToString();

        Assert.Contains("ScriptEvaluationContext", value, StringComparison.Ordinal);
        Assert.Contains("EnemyVisible = True", value, StringComparison.Ordinal);
        Assert.Contains("WeaponReady = True", value, StringComparison.Ordinal);
        Assert.Contains("MyHitPoints = 75", value, StringComparison.Ordinal);
        Assert.Contains("EnemyDistance =", value, StringComparison.Ordinal);
        Assert.Contains("SensorReady = True", value, StringComparison.Ordinal);
        Assert.Contains("TurretAimStatus = NoTarget", value, StringComparison.Ordinal);
    }
}
