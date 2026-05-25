using System;
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

public sealed class MatchStateTankMovementPipelineTests
{
    private static readonly FixedVec2 DefaultPosition = FixedVec2.FromInts(10, 20);
    private static readonly FixedVec2 UnitVelocityEast = FixedVec2.FromInts(1, 0);
    private static readonly FixedVec2 ExpectedMovedPosition = FixedVec2.FromInts(11, 20);

    private static MovementState CreateMovement(FixedVec2 position, FixedVec2 velocity)
        => new MovementState(position, velocity);

    private static TankState CreateTank(
        int id,
        FixedVec2 position,
        FixedVec2 velocity,
        int hitPoints = 100)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(id),
            TankCatalog.BasicTank,
            CreateMovement(position, velocity),
            hitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankState CreateDestroyedTank(int id, FixedVec2 position)
        => CreateTank(id, position, FixedVec2.Zero, hitPoints: 0);

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileState CreateProjectile(int id)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return ProjectileSpawnFactory.Create(
            new ProjectileId(id),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            FixedVec2.FromInts(50, 60),
            FixedVec2.FromInts(2, 0),
            new SimTick(5));
    }

    private static MatchState CreateState(
        SimTick tick,
        TankState[] tanks,
        ProjectileState[]? projectiles = null)
    {
        TankWeaponLoadout[] loadouts = new TankWeaponLoadout[tanks.Length];
        for (int i = 0; i < tanks.Length; i++)
        {
            loadouts[i] = CreateLoadout();
        }

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            loadouts,
            projectiles ?? Array.Empty<ProjectileState>());
    }

    [Fact]
    public void Step_NullState_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchStateTankMovementPipeline.Step(null!));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Step_DestroyedTank_ReturnsSkippedDestroyed()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateDestroyedTank(0, DefaultPosition) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        MatchStateTankMovementRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankMovementStatus.SkippedDestroyed, record.Status);
        Assert.False(record.DidMove);
        Assert.Equal(DefaultPosition, result.FinalState.Tanks[0].Movement.Position);
        Assert.Equal(FixedVec2.Zero, result.FinalState.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(5), result.FinalState.CurrentTick);
    }

    [Fact]
    public void Step_AliveTankWithZeroVelocity_ReturnsStayedStill()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, FixedVec2.Zero) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        MatchStateTankMovementRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankMovementStatus.StayedStill, record.Status);
        Assert.False(record.DidMove);
        Assert.Equal(DefaultPosition, result.FinalState.Tanks[0].Movement.Position);
        Assert.Equal(FixedVec2.Zero, result.FinalState.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Step_AliveTankWithVelocity_ReturnsMoved()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        MatchStateTankMovementRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankMovementStatus.Moved, record.Status);
        Assert.True(record.DidMove);
        Assert.Equal(DefaultPosition, record.InitialMovement.Position);
        Assert.Equal(ExpectedMovedPosition, record.FinalMovement.Position);
        Assert.Equal(UnitVelocityEast, record.FinalMovement.VelocityPerTick);
        Assert.Equal(ExpectedMovedPosition, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_MixedTanks_ProducesRecordsInTankIndexOrder()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, UnitVelocityEast),
                CreateTank(1, FixedVec2.FromInts(30, 20), FixedVec2.Zero),
                CreateDestroyedTank(2, FixedVec2.FromInts(40, 20)),
            });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(3, result.Count);
        Assert.Equal(MatchStateTankMovementStatus.Moved, result.GetRecordAtIndex(0).Status);
        Assert.Equal(MatchStateTankMovementStatus.StayedStill, result.GetRecordAtIndex(1).Status);
        Assert.Equal(MatchStateTankMovementStatus.SkippedDestroyed, result.GetRecordAtIndex(2).Status);

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(i, result.GetRecordAtIndex(i).TankIndex);
            Assert.Equal(state.Tanks[i].Id, result.GetRecordAtIndex(i).TankId);
        }
    }

    [Fact]
    public void Step_ReturnsResultWithInitialAndFinalState()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Same(state, result.InitialState);
        Assert.NotSame(state, result.FinalState);
        Assert.NotSame(result.InitialState, result.FinalState);
    }

    [Fact]
    public void Step_FinalStatePreservesTankCount()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, UnitVelocityEast),
                CreateTank(1, FixedVec2.FromInts(30, 20), FixedVec2.Zero),
            });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(2, result.FinalState.Tanks.Count);
    }

    [Fact]
    public void Step_FinalStatePreservesCurrentTick()
    {
        SimTick tick = new SimTick(12);
        MatchState state = CreateState(
            tick,
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(tick, result.FinalState.CurrentTick);
    }

    [Fact]
    public void Step_FinalStatePreservesProjectiles()
    {
        ProjectileState projectile = CreateProjectile(0);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) },
            new[] { projectile });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(state.Projectiles.Count, result.FinalState.Projectiles.Count);
        Assert.Equal(projectile, result.FinalState.Projectiles[0]);
    }

    [Fact]
    public void Step_FinalStatePreservesArenaReference()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Same(ArenaCatalog.OpenTestArena, result.FinalState.Arena);
    }

    [Fact]
    public void Step_DoesNotMutateInputState()
    {
        TankState tank = CreateTank(0, DefaultPosition, UnitVelocityEast);
        MovementState movementBefore = tank.Movement;
        MatchState state = CreateState(new SimTick(5), new[] { tank });

        _ = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(movementBefore, state.Tanks[0].Movement);
        Assert.Equal(DefaultPosition, state.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_RepeatedCalls_ReturnEquivalentResults()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, UnitVelocityEast),
                CreateTank(1, FixedVec2.FromInts(30, 20), FixedVec2.Zero),
            });

        MatchStateTankMovementResult first = MatchStateTankMovementPipeline.Step(state);
        MatchStateTankMovementResult second = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(first.Count, second.Count);

        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first.GetRecordAtIndex(i).Status, second.GetRecordAtIndex(i).Status);
            Assert.Equal(
                first.FinalState.Tanks[i].Movement.Position,
                second.FinalState.Tanks[i].Movement.Position);
            Assert.Equal(
                first.FinalState.Tanks[i].Movement.VelocityPerTick,
                second.FinalState.Tanks[i].Movement.VelocityPerTick);
        }
    }

    [Fact]
    public void Step_DoesNotAdvanceCurrentTick()
    {
        SimTick tick = new SimTick(7);
        MatchState state = CreateState(
            tick,
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(tick, result.InitialState.CurrentTick);
        Assert.Equal(tick, result.FinalState.CurrentTick);
    }

    [Fact]
    public void Step_DoesNotMoveProjectiles()
    {
        ProjectileState projectile = CreateProjectile(0);
        FixedVec2 positionBefore = projectile.Position;
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, UnitVelocityEast) },
            new[] { projectile });

        MatchStateTankMovementResult result = MatchStateTankMovementPipeline.Step(state);

        Assert.Equal(positionBefore, result.FinalState.Projectiles[0].Position);
    }
}
