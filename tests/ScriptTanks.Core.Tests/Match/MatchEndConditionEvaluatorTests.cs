using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchEndConditionEvaluatorTests
{
    private static TankState CreateTank(int id, int ownerSlot, int? hp = null)
    {
        MovementState movement = new MovementState(
            FixedVec2.FromInts(10 + id, 20),
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

    private static MatchState CreateState(SimTick tick, params TankState[] tanks)
    {
        TankWeaponLoadout[] loadouts = tanks
            .Select(_ => CreateLoadout())
            .ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            loadouts,
            Array.Empty<ScriptTanks.Core.Projectiles.ProjectileState>());
    }

    // -------------------- Result Factories --------------------

    [Fact]
    public void RunningFactory_ReturnsNonEndedNoneWithoutWinner()
    {
        MatchEndConditionResult result = MatchEndConditionResult.Running();

        Assert.False(result.IsEnded);
        Assert.Equal(MatchEndReason.None, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    [Fact]
    public void TankDestroyedFactory_ReturnsEndedWinner()
    {
        TankState winner = CreateTank(3, 1);

        MatchEndConditionResult result = MatchEndConditionResult.TankDestroyed(winner);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.Reason);
        Assert.True(result.WinnerTankId.HasValue);
        Assert.Equal(winner.Id, result.WinnerTankId!.Value);
        Assert.True(result.WinnerSlot.HasValue);
        Assert.Equal(winner.OwnerSlot, result.WinnerSlot!.Value);
    }

    [Fact]
    public void TimeoutHpAdvantageFactory_ReturnsEndedWinner()
    {
        TankState winner = CreateTank(7, 0, hp: 42);

        MatchEndConditionResult result = MatchEndConditionResult.TimeoutHpAdvantage(winner);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutHpAdvantage, result.Reason);
        Assert.True(result.WinnerTankId.HasValue);
        Assert.Equal(winner.Id, result.WinnerTankId!.Value);
        Assert.True(result.WinnerSlot.HasValue);
        Assert.Equal(winner.OwnerSlot, result.WinnerSlot!.Value);
    }

    [Fact]
    public void TimeoutDrawFactory_ReturnsEndedWithoutWinner()
    {
        MatchEndConditionResult result = MatchEndConditionResult.TimeoutDraw();

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Evaluate_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchEndConditionEvaluator.Evaluate(null!, maxTicks: 100));
    }

    [Fact]
    public void Evaluate_RejectsNegativeMaxTicks()
    {
        MatchState state = CreateState(new SimTick(0), CreateTank(0, 0), CreateTank(1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchEndConditionEvaluator.Evaluate(state, maxTicks: -1));
    }

    // -------------------- Running / Destruction --------------------

    [Fact]
    public void Evaluate_ReturnsRunning_WhenNoTankDestroyedAndBeforeTimeout()
    {
        MatchState state = CreateState(
            new SimTick(5),
            CreateTank(0, 0),
            CreateTank(1, 1));

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.False(result.IsEnded);
        Assert.Equal(MatchEndReason.None, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    [Fact]
    public void Evaluate_ReturnsTankDestroyed_WhenExactlyOneTankSurvives()
    {
        TankState alive = CreateTank(0, 0, hp: 100);
        TankState destroyed = CreateTank(1, 1, hp: 0);
        MatchState state = CreateState(new SimTick(5), alive, destroyed);

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.Reason);
        Assert.True(result.WinnerTankId.HasValue);
        Assert.Equal(alive.Id, result.WinnerTankId!.Value);
        Assert.True(result.WinnerSlot.HasValue);
        Assert.Equal(alive.OwnerSlot, result.WinnerSlot!.Value);
    }

    [Fact]
    public void Evaluate_ReturnsRunning_WhenMultipleTanksSurviveDespiteDestroyedTank()
    {
        MatchState state = CreateState(
            new SimTick(5),
            CreateTank(0, 0, hp: 100),
            CreateTank(1, 1, hp: 80),
            CreateTank(2, 2, hp: 0));

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.False(result.IsEnded);
        Assert.Equal(MatchEndReason.None, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    [Fact]
    public void Evaluate_ReturnsRunning_WhenAllTanksDestroyedBeforeTimeout()
    {
        MatchState state = CreateState(
            new SimTick(5),
            CreateTank(0, 0, hp: 0),
            CreateTank(1, 1, hp: 0));

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.False(result.IsEnded);
        Assert.Equal(MatchEndReason.None, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    // -------------------- Timeout --------------------

    [Fact]
    public void Evaluate_ReturnsTimeoutHpAdvantage_WhenTickReachedAndOneTankHasHighestHp()
    {
        TankState leader = CreateTank(0, 0, hp: 80);
        TankState trailing = CreateTank(1, 1, hp: 50);
        MatchState state = CreateState(new SimTick(100), leader, trailing);

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutHpAdvantage, result.Reason);
        Assert.True(result.WinnerTankId.HasValue);
        Assert.Equal(leader.Id, result.WinnerTankId!.Value);
        Assert.True(result.WinnerSlot.HasValue);
        Assert.Equal(leader.OwnerSlot, result.WinnerSlot!.Value);
    }

    [Fact]
    public void Evaluate_ReturnsTimeoutDraw_WhenTickReachedAndHighestHpTied()
    {
        MatchState state = CreateState(
            new SimTick(100),
            CreateTank(0, 0, hp: 50),
            CreateTank(1, 1, hp: 50));

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    [Fact]
    public void Evaluate_AllowsMaxTicksZero()
    {
        MatchState state = CreateState(
            new SimTick(0),
            CreateTank(0, 0),
            CreateTank(1, 1));

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 0);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TimeoutDraw, result.Reason);
        Assert.False(result.WinnerTankId.HasValue);
        Assert.False(result.WinnerSlot.HasValue);
    }

    // -------------------- Precedence and Purity --------------------

    [Fact]
    public void Evaluate_PrefersTankDestroyedOverTimeout()
    {
        TankState alive = CreateTank(0, 0, hp: 50);
        TankState destroyed = CreateTank(1, 1, hp: 0);
        MatchState state = CreateState(new SimTick(100), alive, destroyed);

        MatchEndConditionResult result = MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.True(result.IsEnded);
        Assert.Equal(MatchEndReason.TankDestroyed, result.Reason);
        Assert.True(result.WinnerTankId.HasValue);
        Assert.Equal(alive.Id, result.WinnerTankId!.Value);
        Assert.True(result.WinnerSlot.HasValue);
        Assert.Equal(alive.OwnerSlot, result.WinnerSlot!.Value);
    }

    [Fact]
    public void Evaluate_DoesNotMutateOriginalState()
    {
        TankState tank0 = CreateTank(0, 0, hp: 80);
        TankState tank1 = CreateTank(1, 1, hp: 0);
        MatchState state = CreateState(new SimTick(50), tank0, tank1);
        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];
        int originalProjectileCount = state.Projectiles.Count;

        MatchEndConditionEvaluator.Evaluate(state, maxTicks: 100);

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTanks.Length, state.Tanks.Count);
        for (int i = 0; i < originalTanks.Length; i++)
        {
            Assert.Equal(originalTanks[i], state.Tanks[i]);
        }
        Assert.Same(originalLoadout0, state.Loadouts[0]);
        Assert.Same(originalLoadout1, state.Loadouts[1]);
        Assert.Equal(originalProjectileCount, state.Projectiles.Count);
    }
}
