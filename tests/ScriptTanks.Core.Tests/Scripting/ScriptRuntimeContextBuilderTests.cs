using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeContextBuilderTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null,
        Fixed? turretRotation = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                position ?? FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: turretRotation ?? Fixed.Zero);
    }

    private static TankWeaponLoadout CreateWeaponLoadout(params WeaponState[] weapons)
    {
        return new TankWeaponLoadout(weapons);
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateSensorLoadout(
        SensorState first,
        SensorState? second = null)
    {
        return second.HasValue
            ? new TankSensorLoadout(new[] { first, second.Value })
            : new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    [Fact]
    public void Build_RejectsNullRuntime_ParamNameRuntime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => ScriptRuntimeContextBuilder.Build(null!, 0));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Build_RejectsNegativeTankIndex_ParamNameTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ScriptRuntimeContextBuilder.Build(runtime, -1));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Build_RejectsPastEndTankIndex_ParamNameTankIndex()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ScriptRuntimeContextBuilder.Build(runtime, 2));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Build_MapsCurrentTankHpToMyHitPoints()
    {
        int hp = 42;
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0), hp));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(hp, ctx.MyHitPoints);
    }

    [Fact]
    public void Build_DestroyedEvaluator_ReturnsSafeContext()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0), hp: 0));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(0, ctx.MyHitPoints);
        Assert.False(ctx.WeaponReady);
        Assert.False(ctx.SensorReady);
        Assert.False(ctx.EnemyVisible);
        Assert.Equal(Fixed.Zero, ctx.EnemyDistance);
        Assert.Equal(ScriptVisibleTurretAimStatus.OwnerDestroyed, ctx.TurretAimStatus.Status);
        Assert.False(ctx.TurretAimStatus.HasTarget);
    }

    [Fact]
    public void BuildContext_TurretAligned_PopulatesTurretAimStatus()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(10, 10), turretRotation: Fixed.Zero),
            CreateTank(1, 1, FixedVec2.FromInts(20, 10)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, ctx.TurretAimStatus.Status);
        Assert.True(ctx.TurretAimStatus.IsAligned);
    }

    [Fact]
    public void BuildContext_TurretTurning_PopulatesTurretAimStatus()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(10, 10), turretRotation: Fixed.Zero),
            CreateTank(1, 1, FixedVec2.FromInts(10, 20)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, ctx.TurretAimStatus.Status);
        Assert.False(ctx.TurretAimStatus.IsAligned);
    }

    [Fact]
    public void BuildContext_NoTarget_PopulatesTurretAimStatus()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.NoTarget, ctx.TurretAimStatus.Status);
        Assert.False(ctx.TurretAimStatus.HasTarget);
    }

    [Fact]
    public void BuildContext_UsesPositionBasedAimTarget_NotSensorEnemyVisible()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(100, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.False(ctx.EnemyVisible);
        Assert.True(ctx.TurretAimStatus.HasTarget);
    }

    [Fact]
    public void Build_ReadyWeapon_ProducesWeaponReadyTrue()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.True(ctx.WeaponReady);
    }

    [Fact]
    public void Build_AllWeaponsOnCooldown_ProducesWeaponReadyFalse()
    {
        SimTick tick = new SimTick(10);
        WeaponState w1 = WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(tick);
        WeaponState w2 = WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(tick);
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(w1, w2),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.False(ctx.WeaponReady);
    }

    [Fact]
    public void Build_ReadySensor_ProducesSensorReadyTrue()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.True(ctx.SensorReady);
    }

    [Fact]
    public void Build_AllSensorsOnCooldown_ProducesSensorReadyFalse()
    {
        SimTick tick = new SimTick(10);
        SensorState s1 = new SensorState(SensorCatalog.BasicRadar, tick);
        SensorState s2 = new SensorState(SensorCatalog.WideScanner, tick);
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(s1, s2));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.False(ctx.SensorReady);
    }

    [Fact]
    public void Build_NoVisibleEnemies_ProducesEnemyVisibleFalse()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(200, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.False(ctx.EnemyVisible);
    }

    [Fact]
    public void Build_NoVisibleEnemies_ProducesEnemyDistanceZero()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(200, 0)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(Fixed.Zero, ctx.EnemyDistance);
    }

    [Fact]
    public void Build_VisibleEnemy_ProducesEnemyVisibleTrue()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.True(ctx.EnemyVisible);
    }

    [Fact]
    public void Build_VisibleEnemyDistance_UsesNearestEnemyAndThreeFourFiveDistance()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)),
            CreateTank(1, 1, FixedVec2.FromInts(3, 4)),
            CreateTank(2, 1, FixedVec2.FromInts(0, 12)));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.True(ctx.EnemyVisible);
        Assert.Equal(Fixed.FromInt(5), ctx.EnemyDistance);
    }

    [Fact]
    public void Build_DoesNotMutateRuntimeState()
    {
        SimTick tick = new SimTick(10);
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(3, 4));
        SensorState sensorSnapshot =
            SensorState.Ready(SensorCatalog.BasicRadar);
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            tank0,
            tank1);
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(sensorSnapshot),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tank0Before = runtime.State.Tanks[0];
        TankState tank1Before = runtime.State.Tanks[1];
        SensorState sensorBefore = runtime.SensorLoadouts
            .GetLoadoutAtIndex(0)
            .GetSensor(SensorSlot.Zero);

        ScriptEvaluationContext ctx = ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tank0Before, runtime.State.Tanks[0]);
        Assert.Equal(tank1Before, runtime.State.Tanks[1]);
        Assert.Equal(sensorBefore, runtime.SensorLoadouts.GetLoadoutAtIndex(0).GetSensor(SensorSlot.Zero));
        Assert.True(ctx.EnemyVisible);
    }
}
