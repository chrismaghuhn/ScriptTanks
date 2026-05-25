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

public sealed class MatchRunnerScheduledFireTests
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

    private static MatchFireRequest CreateRequest(
        int shooterId = 0,
        int weaponSlot = 0,
        int projectileId = 123,
        FixedVec2? muzzle = null,
        FixedVec2? velocity = null)
        => new MatchFireRequest(
            shooterTankId: new TankId(shooterId),
            weaponSlot: new WeaponSlot(weaponSlot),
            projectileId: new ProjectileId(projectileId),
            muzzlePosition: muzzle ?? FixedVec2.FromInts(50, 50),
            fireVelocity: velocity ?? new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

    private static MatchScheduledFireRequest CreateScheduled(
        int tickValue,
        MatchFireRequest? request = null)
        => new MatchScheduledFireRequest(
            new SimTick(tickValue),
            request ?? CreateRequest());

    // -------------------- Validation --------------------

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchRunner.RunUntilEnd(
                initialState: null!,
                maxTicks: 100,
                scheduledFireRequests: Array.Empty<MatchScheduledFireRequest>()));
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsNullSchedule()
    {
        MatchState state = CreateState(new SimTick(0));

        Assert.Throws<ArgumentNullException>(
            () => MatchRunner.RunUntilEnd(
                initialState: state,
                maxTicks: 100,
                scheduledFireRequests: null!));
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchRunner.RunUntilEnd(
                initialState: state,
                maxTicks: -1,
                scheduledFireRequests: Array.Empty<MatchScheduledFireRequest>()));
        Assert.Equal("maxTicks", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsScheduleEarlierThanInitialTick()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(4),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchRunner.RunUntilEnd(state, maxTicks: 100, schedule));
        Assert.Equal("scheduledFireRequests", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsUnorderedSchedule()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(0),
            CreateScheduled(2),
            CreateScheduled(1),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchRunner.RunUntilEnd(state, maxTicks: 100, schedule));
        Assert.Equal("scheduledFireRequests", ex.ParamName);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_RejectsDuplicateTick()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(1),
            CreateScheduled(1),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MatchRunner.RunUntilEnd(state, maxTicks: 100, schedule));
        Assert.Equal("scheduledFireRequests", ex.ParamName);
    }

    // -------------------- Behavior --------------------

    [Fact]
    public void RunUntilEnd_WithScheduledFire_ReturnsImmediately_WhenInitialStateAlreadyEnded()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(0, CreateRequest()),
        };

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 0, schedule);

        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.EndCondition.Reason);
        Assert.Equal(0, result.TicksExecuted);
        Assert.Same(state, result.FinalState);
        Assert.Empty(result.FinalState.Projectiles);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_AppliesRequestAtMatchingTick()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchFireRequest request = CreateRequest(
            muzzle: FixedVec2.FromInts(50, 50),
            velocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(0, request),
        };

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 2, schedule);

        Assert.Equal(2, result.TicksExecuted);
        Assert.Equal(new SimTick(2), result.FinalState.CurrentTick);
        Assert.Single(result.FinalState.Projectiles);
        Assert.Equal(
            FixedVec2.FromInts(52, 50),
            result.FinalState.Projectiles[0].Position);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_DoesNotApplyFutureRequestBeforeTimeout()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(5, CreateRequest()),
        };

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 2, schedule);

        Assert.Equal(2, result.TicksExecuted);
        Assert.Equal(new SimTick(2), result.FinalState.CurrentTick);
        Assert.Empty(result.FinalState.Projectiles);
        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.EndCondition.Reason);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_StopsWhenScheduledFireDestroysTank()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 20);
        MatchState state = CreateState(
            new SimTick(0),
            tanks: new[] { tank0, tank1 });

        MatchFireRequest request = CreateRequest(
            shooterId: 0,
            muzzle: FixedVec2.FromInts(87, 20),
            velocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(0, request),
        };

        MatchRunResult result = MatchRunner.RunUntilEnd(state, maxTicks: 100, schedule);

        Assert.Equal(1, result.TicksExecuted);
        Assert.True(result.EndCondition.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.EndCondition.Reason);
        Assert.True(result.EndCondition.WinnerTankId.HasValue);
        Assert.Equal(new TankId(0), result.EndCondition.WinnerTankId!.Value);
        Assert.Equal(new SimTick(1), result.FinalState.CurrentTick);
        Assert.Empty(result.FinalState.Projectiles);

        TankState updatedTank1 = result.FinalState.Tanks
            .First(t => t.Id == new TankId(1));
        Assert.Equal(0, updatedTank1.CurrentHitPoints);
    }

    [Fact]
    public void RunUntilEnd_WithScheduledFire_DoesNotMutateInitialState()
    {
        MatchState state = CreateState(new SimTick(0));
        MatchFireRequest request = CreateRequest(
            muzzle: FixedVec2.FromInts(50, 50),
            velocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));
        MatchScheduledFireRequest[] schedule = new[]
        {
            CreateScheduled(0, request),
        };

        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        ProjectileState[] originalProjectiles = state.Projectiles.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];
        int originalProjectileCount = state.Projectiles.Count;

        MatchRunner.RunUntilEnd(state, maxTicks: 2, schedule);

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
