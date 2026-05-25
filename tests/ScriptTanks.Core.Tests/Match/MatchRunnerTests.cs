using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchRunnerTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        MovementState movement = new MovementState(
            position ?? FixedVec2.FromInts(10 + id, 20),
            FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileDefinition CreateProjectileDefinition(
        int rawDamage = 25,
        Fixed? speedPerTick = null,
        Fixed? maxRange = null,
        Fixed? radius = null)
        => new ProjectileDefinition(
            rawDamage,
            speedPerTick ?? Fixed.FromInt(1),
            maxRange ?? Fixed.FromInt(40),
            radius ?? Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(
        int id,
        FixedVec2? position = null,
        FixedVec2? velocityPerTick = null,
        Fixed? remainingRange = null,
        bool isActive = true,
        ProjectileDefinition? definition = null,
        int ownerTankId = 99)
    {
        ProjectileDefinition projectileDefinition =
            definition ?? CreateProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(id),
            projectileDefinition,
            new TankId(ownerTankId),
            new WeaponSlot(0),
            new SimTick(0),
            position ?? FixedVec2.FromInts(50, 50),
            velocityPerTick ?? FixedVec2.FromInts(1, 0),
            remainingRange ?? projectileDefinition.MaxRange,
            isActive);
    }

    private static MatchState CreateState(
        SimTick tick,
        TankState[]? tanks = null,
        ProjectileState[]? projectiles = null)
    {
        TankState[] actualTanks = tanks ?? new[]
        {
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
        };
        TankWeaponLoadout[] loadouts = actualTanks
            .Select(_ => CreateLoadout())
            .ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            actualTanks,
            loadouts,
            projectiles ?? Array.Empty<ProjectileState>());
    }

    // -------------------- Validation --------------------

    [Fact]
    public void MatchRunResult_RejectsNullFinalState()
    {
        MatchEndConditionResult endCondition = MatchEndConditionResult.Running();

        Assert.Throws<ArgumentNullException>(
            () => new MatchRunResult(null!, endCondition, 0));
    }

    [Fact]
    public void MatchRunResult_RejectsNullEndCondition()
    {
        MatchState state = CreateState(new SimTick(0));

        Assert.Throws<ArgumentNullException>(
            () => new MatchRunResult(state, null!, 0));
    }

    [Fact]
    public void MatchRunResult_RejectsNegativeTicksExecuted()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchEndConditionResult endCondition = MatchEndConditionResult.Running();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchRunResult(state, endCondition, -1));
        Assert.Equal("ticksExecuted", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchRunner.RunUntilEnd(null!, maxTicks: 100));
    }

    [Fact]
    public void RunUntilEnd_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchRunner.RunUntilEnd(state, maxTicks: -1));
        Assert.Equal("maxTicks", ex.ParamName);
    }

    // -------------------- Initial-state termination --------------------

    [Fact]
    public void RunUntilEnd_ReturnsImmediately_WhenInitialStateAlreadyEndedByTimeout()
    {
        MatchState state = CreateState(new SimTick(0));

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 0);

        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.EndCondition.Reason);
        Assert.Equal(0, result.TicksExecuted);
        Assert.Equal(new SimTick(0), result.FinalState.CurrentTick);
        Assert.Same(state, result.FinalState);
    }

    [Fact]
    public void RunUntilEnd_ReturnsImmediately_WhenInitialStateAlreadyEndedByTankDestroyed()
    {
        TankState alive = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState destroyed = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 0);
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { alive, destroyed });

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 100);

        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.EndCondition.Reason);
        Assert.True(result.EndCondition.WinnerTankId.HasValue);
        Assert.Equal(alive.Id, result.EndCondition.WinnerTankId!.Value);
        Assert.Equal(0, result.TicksExecuted);
        Assert.Equal(new SimTick(0), result.FinalState.CurrentTick);
        Assert.Same(state, result.FinalState);
    }

    // -------------------- Loop behavior --------------------

    [Fact]
    public void RunUntilEnd_AdvancesUntilTimeoutDraw()
    {
        MatchState state = CreateState(new SimTick(0));

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 3);

        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.EndCondition.Reason);
        Assert.Equal(3, result.TicksExecuted);
        Assert.Equal(new SimTick(3), result.FinalState.CurrentTick);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, result.FinalState.Tanks[0].CurrentHitPoints);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, result.FinalState.Tanks[1].CurrentHitPoints);
    }

    [Fact]
    public void RunUntilEnd_StopsWhenProjectileDestroysTank()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 20);
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(87, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            ownerTankId: 0);
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { tank0, tank1 },
            projectiles: new[] { projectile });

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 100);

        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.EndCondition.Reason);
        Assert.True(result.EndCondition.WinnerTankId.HasValue);
        Assert.Equal(tank0.Id, result.EndCondition.WinnerTankId!.Value);
        Assert.Equal(1, result.TicksExecuted);
        Assert.Equal(new SimTick(1), result.FinalState.CurrentTick);
        Assert.Empty(result.FinalState.Projectiles);
        Assert.Equal(0, result.FinalState.Tanks[1].CurrentHitPoints);
    }

    [Fact]
    public void RunUntilEnd_ReportsTicksExecuted()
    {
        MatchState state = CreateState(new SimTick(0));

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 5);

        Assert.Equal(5, result.TicksExecuted);
        Assert.Equal(new SimTick(5), result.FinalState.CurrentTick);
    }

    [Fact]
    public void RunUntilEnd_ReturnsFinalStateAtEnd()
    {
        MatchState initial = CreateState(new SimTick(0));

        MatchRunResult result = MatchRunner.RunUntilEnd(initial, maxTicks: 3);

        Assert.True(result.EndCondition.IsEnded);
        Assert.NotSame(initial, result.FinalState);
        Assert.Equal(new SimTick(3), result.FinalState.CurrentTick);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void RunUntilEnd_DoesNotMutateInitialState()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        ProjectileState projectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { tank0, tank1 },
            projectiles: new[] { projectile });

        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        ProjectileState[] originalProjectiles = state.Projectiles.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];
        int originalProjectileCount = state.Projectiles.Count;

        MatchRunner.RunUntilEnd(state, maxTicks: 3);

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTanks.Length, state.Tanks.Count);
        for (int i = 0; i < originalTanks.Length; i++)
        {
            Assert.Equal(originalTanks[i], state.Tanks[i]);
        }
        Assert.Equal(originalProjectileCount, state.Projectiles.Count);
        for (int i = 0; i < originalProjectiles.Length; i++)
        {
            Assert.Equal(originalProjectiles[i], state.Projectiles[i]);
        }
        Assert.Same(originalLoadout0, state.Loadouts[0]);
        Assert.Same(originalLoadout1, state.Loadouts[1]);
    }
}
