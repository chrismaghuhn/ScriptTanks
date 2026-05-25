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

public sealed class MatchTickProjectilePipelineTests
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
    public void StepProjectilesAndResolveHits_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchTickProjectilePipeline.StepProjectilesAndResolveHits(null!));
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_PreservesArenaReference()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void StepProjectilesAndResolveHits_PreservesCurrentTick()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Equal(state.CurrentTick, updated.CurrentTick);
    }

    [Fact]
    public void StepProjectilesAndResolveHits_PreservesLoadouts()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void StepProjectilesAndResolveHits_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateState(CreateProjectile(1));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- Pipeline Ordering --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_MovesProjectileBeforeHitDetection()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(7, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0)));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Empty(updated.Projectiles);
        Assert.Equal(80, updated.Tanks[0].CurrentHitPoints);
    }

    // -------------------- Cleanup After Range Depletion --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_RemovesProjectileDeactivatedByRangeDepletion()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(1)));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Empty(updated.Projectiles);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[1].CurrentHitPoints);
    }

    // -------------------- No Hit --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_KeepsMovedProjectile_WhenNoHitAndStillActive()
    {
        MatchState state = CreateState(CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: Fixed.FromInt(40)));

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        ProjectileState remaining = Assert.Single(updated.Projectiles);
        Assert.Equal(FixedVec2.FromInts(51, 50), remaining.Position);
        Assert.Equal(Fixed.FromInt(39), remaining.RemainingRange);
        Assert.True(remaining.IsActive);
    }

    // -------------------- Multiple Projectiles --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_HandlesHitAndMissInSamePipeline()
    {
        ProjectileState a = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(7, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        ProjectileState b = CreateProjectile(
            id: 2,
            position: FixedVec2.FromInts(50, 50),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        MatchState state = CreateState(a, b);

        MatchState updated = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        ProjectileState remaining = Assert.Single(updated.Projectiles);
        Assert.Equal(new ProjectileId(2), remaining.Id);
        Assert.Equal(FixedVec2.FromInts(51, 50), remaining.Position);
        Assert.True(remaining.IsActive);
        Assert.Equal(80, updated.Tanks[0].CurrentHitPoints);
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, updated.Tanks[1].CurrentHitPoints);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void StepProjectilesAndResolveHits_DoesNotMutateOriginalState()
    {
        ProjectileState originalProjectile = CreateProjectile(
            id: 1,
            position: FixedVec2.FromInts(7, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0));
        TankState originalTank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState originalTank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));
        MatchState state = CreateState(originalProjectile);
        ArenaDefinition originalArena = state.Arena;
        SimTick originalTick = state.CurrentTick;

        MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);

        Assert.Single(state.Projectiles);
        Assert.Equal(originalProjectile, state.Projectiles[0]);
        Assert.Equal(originalTank0, state.Tanks[0]);
        Assert.Equal(originalTank1, state.Tanks[1]);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTick, state.CurrentTick);
    }
}
