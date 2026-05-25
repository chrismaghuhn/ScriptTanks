using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
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

public sealed class MatchTickPipelineTests
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

    private static MatchState CreateState(params ProjectileState[] projectiles)
        => new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
                CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
            },
            new[] { CreateLoadout(), CreateLoadout() },
            projectiles);

    // -------------------- Validation --------------------

    [Fact]
    public void Step_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchTickPipeline.Step(null!));
    }

    // -------------------- Tick --------------------

    [Fact]
    public void Step_AdvancesCurrentTickByOne()
    {
        MatchState state = CreateState();

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    // -------------------- Pipeline Ordering --------------------

    [Fact]
    public void Step_RunsProjectilePipelineBeforeTickAdvance()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(7, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0)));

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Empty(updated.Projectiles);
        Assert.Equal(80, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void Step_PreservesArenaReference()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void Step_PreservesLoadouts()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void Step_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- No Hit --------------------

    [Fact]
    public void Step_KeepsMovedProjectile_WhenNoHitAndStillActive()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40)));

        MatchState updated = MatchTickPipeline.Step(state);

        ProjectileState remaining = Assert.Single(updated.Projectiles);
        Assert.Equal(FixedVec2.FromInts(51, 50), remaining.Position);
        Assert.Equal(Fixed.FromInt(39), remaining.RemainingRange);
        Assert.True(remaining.IsActive);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    // -------------------- Cleanup --------------------

    [Fact]
    public void Step_RemovesProjectileDeactivatedByRangeDepletion()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(1)));

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Empty(updated.Projectiles);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[1].CurrentHitPoints);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    // -------------------- Movement before hit (5.117 legacy) --------------------

    private static FixedVec2 MovementBeforeHitTargetStartPosition()
        => new FixedVec2(Fixed.FromRatio(77, 10), Fixed.FromInt(20));

    private static MatchState CreateMovementBeforeHitLegacyState(SimTick tick)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        FixedVec2 projectilePosition = FixedVec2.FromInts(10, 20);
        ProjectileState projectile = new ProjectileState(
            new ProjectileId(0),
            definition,
            new TankId(1),
            new WeaponSlot(0),
            new SimTick(tick.Value - 1),
            projectilePosition,
            FixedVec2.Zero,
            definition.MaxRange,
            isActive: true);

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            new[]
            {
                CreateTank(0, 0, MovementBeforeHitTargetStartPosition()),
                CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
            },
            new[] { CreateLoadout(), CreateLoadout() },
            new[] { projectile });
    }

    [Fact]
    public void Step_ProjectilesUsePreExistingTankPositions_WhenLegacyTickDoesNotMoveTanks()
    {
        SimTick startTick = new SimTick(10);
        MatchState state = CreateMovementBeforeHitLegacyState(startTick);

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(MovementBeforeHitTargetStartPosition(), updated.Tanks[0].Movement.Position);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.Single(updated.Projectiles);
        Assert.True(updated.Projectiles[0].IsActive);
        Assert.Equal(new SimTick(11), updated.CurrentTick);
    }

    // -------------------- Tank movement (5.115 lock) --------------------

    [Fact]
    public void Step_DoesNotIntegrateTankMovement()
    {
        FixedVec2 position = FixedVec2.FromInts(10, 20);
        FixedVec2 velocity = FixedVec2.FromInts(1, 0) * TankCatalog.BasicTank.Stats.MaxVelocityPerTick;
        TankState tankWithVelocity = new TankState(
            new TankId(0),
            new PlayerSlot(0),
            TankCatalog.BasicTank,
            new MovementState(position, velocity),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { tankWithVelocity, CreateTank(1, 1, FixedVec2.FromInts(90, 20)) },
            new[] { CreateLoadout(), CreateLoadout() },
            Array.Empty<ProjectileState>());

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(position, updated.Tanks[0].Movement.Position);
        Assert.Equal(velocity, updated.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    [Fact]
    public void Step_DoesNotApplyTankBoundsClamp()
    {
        FixedVec2 outsidePosition = FixedVec2.FromInts(-1, 20);
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0, outsidePosition) },
            new[] { CreateLoadout() },
            Array.Empty<ProjectileState>());

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(outsidePosition, updated.Tanks[0].Movement.Position);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    [Fact]
    public void Step_ProjectilesUseUnboundedTankPosition_WhenLegacyTickDoesNotClampBounds()
    {
        SimTick startTick = new SimTick(10);
        FixedVec2 rawOutsidePosition = FixedVec2.FromInts(99, 20);
        ProjectileDefinition definition = CreateProjectileDefinition();
        FixedVec2 projectilePosition = new FixedVec2(Fixed.FromRatio(479, 5), Fixed.FromInt(20));

        ProjectileState projectile = new ProjectileState(
            new ProjectileId(0),
            definition,
            new TankId(1),
            new WeaponSlot(0),
            new SimTick(startTick.Value - 1),
            projectilePosition,
            FixedVec2.Zero,
            definition.MaxRange,
            isActive: true);

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            startTick,
            new[]
            {
                CreateTank(0, 0, rawOutsidePosition),
                CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
            },
            new[] { CreateLoadout(), CreateLoadout() },
            new[] { projectile });

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(rawOutsidePosition, updated.Tanks[0].Movement.Position);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(new SimTick(11), updated.CurrentTick);
    }

    [Fact]
    public void Step_DoesNotApplyTankObstacleCollision_WhenLegacyTickRuns()
    {
        FixedVec2 candidatePosition = FixedVec2.FromInts(8, 15);
        FixedVec2 velocity = FixedVec2.FromInts(3, 0);
        ArenaDefinition arena = CreateLegacyWallArena();
        MatchState state = new MatchState(
            arena,
            new SimTick(5),
            new[]
            {
                new TankState(
                    new TankId(0),
                    new PlayerSlot(0),
                    TankCatalog.BasicTank,
                    new MovementState(candidatePosition, velocity),
                    TankCatalog.BasicTank.Stats.MaxHitPoints,
                    Fixed.Zero,
                    Fixed.Zero),
            },
            new[] { CreateLoadout() },
            Array.Empty<ProjectileState>());

        MatchState updated = MatchTickPipeline.Step(state);

        Assert.Equal(candidatePosition, updated.Tanks[0].Movement.Position);
        Assert.Equal(velocity, updated.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    private static ArenaDefinition CreateLegacyWallArena()
    {
        return new ArenaDefinition(
            id: "legacy_wall_test_arena",
            displayName: "Legacy Wall Test Arena",
            description: "Arena for legacy MatchTickPipeline obstacle contrast tests.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
            },
            wallBlocks: new[]
            {
                new WallBlock(
                    "wall_a",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(10, 10),
                        Fixed.FromInt(10),
                        Fixed.FromInt(10))),
            },
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    // -------------------- Purity --------------------

    [Fact]
    public void Step_DoesNotMutateOriginalState()
    {
        ProjectileState originalProjectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(7, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        MatchState state = CreateState(originalProjectile);
        ArenaDefinition originalArena = state.Arena;
        SimTick originalTick = state.CurrentTick;
        TankState originalTank0 = state.Tanks[0];
        TankState originalTank1 = state.Tanks[1];
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];

        MatchTickPipeline.Step(state);

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Single(state.Projectiles);
        Assert.Equal(originalProjectile, state.Projectiles[0]);
        Assert.Equal(originalTank0, state.Tanks[0]);
        Assert.Equal(originalTank1, state.Tanks[1]);
        Assert.Same(originalArena, state.Arena);
        Assert.Same(originalLoadout0, state.Loadouts[0]);
        Assert.Same(originalLoadout1, state.Loadouts[1]);
    }
}
