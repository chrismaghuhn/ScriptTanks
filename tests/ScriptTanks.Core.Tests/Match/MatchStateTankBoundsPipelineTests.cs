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

public sealed class MatchStateTankBoundsPipelineTests
{
    private static readonly FixedVec2 DefaultPosition = FixedVec2.FromInts(10, 20);
    private static readonly FixedVec2 UnitVelocityEast = FixedVec2.FromInts(1, 0);
    private static readonly Fixed HitboxRadius = TankCatalog.BasicTank.Stats.HitboxRadius;

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

    private static Fixed ExpectedMinAxis(Fixed arenaExtent)
        => HitboxRadius;

    private static Fixed ExpectedMaxX(MatchState state)
        => state.Arena.Bounds.Width - HitboxRadius;

    private static Fixed ExpectedMaxY(MatchState state)
        => state.Arena.Bounds.Height - HitboxRadius;

    [Fact]
    public void Step_NullState_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchStateTankBoundsPipeline.Step(null!));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Step_AliveTankInsideBounds_ReturnsInsideBounds()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, DefaultPosition, FixedVec2.Zero) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        MatchStateTankBoundsRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.InsideBounds, record.Status);
        Assert.False(record.DidClamp);
        Assert.Equal(DefaultPosition, result.FinalState.Tanks[0].Movement.Position);
        Assert.Equal(FixedVec2.Zero, result.FinalState.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Step_DestroyedTank_ReturnsSkippedDestroyed()
    {
        FixedVec2 outsidePosition = FixedVec2.FromInts(-1, 20);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateDestroyedTank(0, outsidePosition) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        MatchStateTankBoundsRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.SkippedDestroyed, record.Status);
        Assert.False(record.DidClamp);
        Assert.Equal(outsidePosition, result.FinalState.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_TankPastLeftBounds_ClampsToHitboxRadius()
    {
        FixedVec2 outside = FixedVec2.FromInts(-1, 20);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, outside, UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        MatchStateTankBoundsRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, record.Status);
        Assert.True(record.DidClamp);
        Assert.Equal(ExpectedMinAxis(state.Arena.Bounds.Width), result.FinalState.Tanks[0].Movement.Position.X);
        Assert.Equal(outside.Y, result.FinalState.Tanks[0].Movement.Position.Y);
    }

    [Fact]
    public void Step_TankPastRightBounds_ClampsToWidthMinusHitboxRadius()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, FixedVec2.FromInts(101, 20), UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        MatchStateTankBoundsRecord record = result.GetRecordAtIndex(0);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, record.Status);
        Assert.Equal(ExpectedMaxX(state), result.FinalState.Tanks[0].Movement.Position.X);
    }

    [Fact]
    public void Step_TankPastBottomBounds_ClampsToHitboxRadius()
    {
        FixedVec2 outside = FixedVec2.FromInts(10, -1);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, outside, UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, result.GetRecordAtIndex(0).Status);
        Assert.Equal(ExpectedMinAxis(state.Arena.Bounds.Height), result.FinalState.Tanks[0].Movement.Position.Y);
        Assert.Equal(outside.X, result.FinalState.Tanks[0].Movement.Position.X);
    }

    [Fact]
    public void Step_TankPastTopBounds_ClampsToHeightMinusHitboxRadius()
    {
        Fixed outsideY = ArenaCatalog.OpenTestArena.Bounds.Height + Fixed.FromInt(1);
        FixedVec2 outside = new FixedVec2(Fixed.FromInt(10), outsideY);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, outside, UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, result.GetRecordAtIndex(0).Status);
        Assert.Equal(ExpectedMaxY(state), result.FinalState.Tanks[0].Movement.Position.Y);
    }

    [Fact]
    public void Step_TankPastTopLeftCorner_ClampsBothAxes()
    {
        FixedVec2 outside = FixedVec2.FromInts(-1, -1);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, outside, UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        FixedVec2 finalPosition = result.FinalState.Tanks[0].Movement.Position;

        Assert.Equal(MatchStateTankBoundsStatus.Clamped, result.GetRecordAtIndex(0).Status);
        Assert.Equal(ExpectedMinAxis(state.Arena.Bounds.Width), finalPosition.X);
        Assert.Equal(ExpectedMinAxis(state.Arena.Bounds.Height), finalPosition.Y);
    }

    [Fact]
    public void Step_ClampedTank_PreservesVelocityPerTick()
    {
        FixedVec2 outside = FixedVec2.FromInts(-1, 20);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, outside, UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(UnitVelocityEast, result.FinalState.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(UnitVelocityEast, state.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Step_FinalStatePreservesCurrentTick()
    {
        SimTick tick = new SimTick(12);
        MatchState state = CreateState(
            tick,
            new[] { CreateTank(0, FixedVec2.FromInts(-1, 20), UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(tick, result.FinalState.CurrentTick);
    }

    [Fact]
    public void Step_FinalStatePreservesArenaReference()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, FixedVec2.FromInts(-1, 20), UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Same(ArenaCatalog.OpenTestArena, result.FinalState.Arena);
    }

    [Fact]
    public void Step_FinalStatePreservesProjectiles()
    {
        ProjectileState projectile = CreateProjectile(0);
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, FixedVec2.FromInts(-1, 20), UnitVelocityEast) },
            new[] { projectile });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(state.Projectiles.Count, result.FinalState.Projectiles.Count);
        Assert.Equal(projectile, result.FinalState.Projectiles[0]);
    }

    [Fact]
    public void Step_FinalStatePreservesTankCount()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, FixedVec2.Zero),
                CreateTank(1, FixedVec2.FromInts(-1, 20), UnitVelocityEast),
            });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(2, result.FinalState.Tanks.Count);
    }

    [Fact]
    public void Step_DoesNotMutateInputState()
    {
        FixedVec2 outside = FixedVec2.FromInts(-1, 20);
        TankState tank = CreateTank(0, outside, UnitVelocityEast);
        MovementState movementBefore = tank.Movement;
        MatchState state = CreateState(new SimTick(5), new[] { tank });

        _ = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(movementBefore, state.Tanks[0].Movement);
        Assert.Equal(outside, state.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Step_ReturnsResultWithInitialAndFinalState()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[] { CreateTank(0, FixedVec2.FromInts(-1, 20), UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Same(state, result.InitialState);
        Assert.NotSame(state, result.FinalState);
        Assert.NotSame(result.InitialState, result.FinalState);
    }

    [Fact]
    public void Step_RepeatedCalls_ReturnEquivalentResults()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, FixedVec2.Zero),
                CreateTank(1, FixedVec2.FromInts(-1, 20), UnitVelocityEast),
            });

        MatchStateTankBoundsResult first = MatchStateTankBoundsPipeline.Step(state);
        MatchStateTankBoundsResult second = MatchStateTankBoundsPipeline.Step(state);

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
    public void Step_ResultRecordsMatchInitialAndFinalTankPositions()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, FixedVec2.Zero),
                CreateTank(1, FixedVec2.FromInts(-1, 20), UnitVelocityEast),
            });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        for (int i = 0; i < result.Count; i++)
        {
            MatchStateTankBoundsRecord record = result.GetRecordAtIndex(i);

            Assert.Equal(result.InitialState.Tanks[i].Movement.Position, record.InitialPosition);
            Assert.Equal(result.FinalState.Tanks[i].Movement.Position, record.FinalPosition);
        }
    }

    [Fact]
    public void Step_MixedTanks_ProducesRecordsInTankIndexOrder()
    {
        MatchState state = CreateState(
            new SimTick(5),
            new[]
            {
                CreateTank(0, DefaultPosition, FixedVec2.Zero),
                CreateTank(1, FixedVec2.FromInts(-1, 20), UnitVelocityEast),
                CreateDestroyedTank(2, FixedVec2.FromInts(99, 99)),
            });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(3, result.Count);
        Assert.Equal(MatchStateTankBoundsStatus.InsideBounds, result.GetRecordAtIndex(0).Status);
        Assert.Equal(MatchStateTankBoundsStatus.Clamped, result.GetRecordAtIndex(1).Status);
        Assert.Equal(MatchStateTankBoundsStatus.SkippedDestroyed, result.GetRecordAtIndex(2).Status);

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(i, result.GetRecordAtIndex(i).TankIndex);
            Assert.Equal(state.Tanks[i].Id, result.GetRecordAtIndex(i).TankId);
        }
    }

    [Fact]
    public void Step_DoesNotAdvanceCurrentTick()
    {
        SimTick tick = new SimTick(7);
        MatchState state = CreateState(
            tick,
            new[] { CreateTank(0, FixedVec2.FromInts(-1, 20), UnitVelocityEast) });

        MatchStateTankBoundsResult result = MatchStateTankBoundsPipeline.Step(state);

        Assert.Equal(tick, result.InitialState.CurrentTick);
        Assert.Equal(tick, result.FinalState.CurrentTick);
    }
}
