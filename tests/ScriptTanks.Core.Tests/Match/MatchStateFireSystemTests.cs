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

public sealed class MatchStateFireSystemTests
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

    private static TankWeaponLoadout CreateReadyLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static TankWeaponLoadout CreateFiredLoadout(SimTick lastFireTick)
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(lastFireTick),
        });

    private static ProjectileState CreateProjectileForState(int id)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return new ProjectileState(
            new ProjectileId(id),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(0),
            FixedVec2.FromInts(50, 50),
            FixedVec2.FromInts(1, 0),
            definition.MaxRange,
            isActive: true);
    }

    private static MatchState CreateState(
        SimTick tick,
        TankWeaponLoadout[]? loadouts = null,
        ProjectileState[]? projectiles = null)
    {
        TankState[] tanks = new[]
        {
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
        };
        TankWeaponLoadout[] actualLoadouts = loadouts
            ?? new[] { CreateReadyLoadout(), CreateReadyLoadout() };

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            actualLoadouts,
            projectiles ?? Array.Empty<ProjectileState>());
    }

    // -------------------- Outcome model --------------------

    [Fact]
    public void MatchStateFireOutcome_NotReadyRejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateFireOutcome.NotReady(null!));
    }

    [Fact]
    public void MatchStateFireOutcome_FiredRejectsNullState()
    {
        ProjectileState projectile = CreateProjectileForState(1);

        Assert.Throws<ArgumentNullException>(
            () => MatchStateFireOutcome.Fired(null!, projectile));
    }

    [Fact]
    public void MatchStateFireOutcome_FiredStoresProjectileAndDidFireTrue()
    {
        MatchState state = CreateState(new SimTick(5));
        ProjectileState projectile = CreateProjectileForState(1);

        MatchStateFireOutcome outcome = MatchStateFireOutcome.Fired(state, projectile);

        Assert.True(outcome.DidFire);
        Assert.True(outcome.HasSpawnedProjectile);
        Assert.True(outcome.SpawnedProjectile.HasValue);
        Assert.Equal(projectile, outcome.SpawnedProjectile!.Value);
        Assert.Same(state, outcome.UpdatedState);
    }

    [Fact]
    public void MatchStateFireOutcome_NotReadyStoresNoProjectile()
    {
        MatchState state = CreateState(new SimTick(5));

        MatchStateFireOutcome outcome = MatchStateFireOutcome.NotReady(state);

        Assert.False(outcome.DidFire);
        Assert.False(outcome.HasSpawnedProjectile);
        Assert.False(outcome.SpawnedProjectile.HasValue);
        Assert.Same(state, outcome.UpdatedState);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void ResolveFire_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateFireSystem.ResolveFire(
                state: null!,
                shooterTankId: new TankId(0),
                weaponSlot: new WeaponSlot(0),
                currentTick: new SimTick(5),
                projectileId: new ProjectileId(123),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero)));
    }

    [Fact]
    public void ResolveFire_RejectsUnknownShooterTankId()
    {
        MatchState state = CreateState(new SimTick(5));

        Assert.Throws<InvalidOperationException>(
            () => MatchStateFireSystem.ResolveFire(
                state,
                shooterTankId: new TankId(99),
                weaponSlot: new WeaponSlot(0),
                currentTick: new SimTick(5),
                projectileId: new ProjectileId(123),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero)));
    }

    [Fact]
    public void ResolveFire_PropagatesInvalidWeaponSlot()
    {
        MatchState state = CreateState(new SimTick(5));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchStateFireSystem.ResolveFire(
                state,
                shooterTankId: new TankId(0),
                weaponSlot: new WeaponSlot(7),
                currentTick: new SimTick(5),
                projectileId: new ProjectileId(123),
                muzzlePosition: FixedVec2.FromInts(10, 20),
                fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero)));
    }

    // -------------------- Ready fire --------------------

    [Fact]
    public void ResolveFire_WhenWeaponReady_AppendsProjectile()
    {
        MatchState state = CreateState(new SimTick(5));
        FixedVec2 muzzlePosition = new FixedVec2(Fixed.FromInt(10), Fixed.FromInt(20));
        FixedVec2 fireVelocity = new FixedVec2(Fixed.FromInt(1), Fixed.Zero);

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: muzzlePosition,
            fireVelocity: fireVelocity);

        Assert.True(outcome.DidFire);
        Assert.True(outcome.HasSpawnedProjectile);
        ProjectileState spawned = outcome.SpawnedProjectile!.Value;
        Assert.Equal(new ProjectileId(123), spawned.Id);
        Assert.Equal(new TankId(0), spawned.OwnerTankId);
        Assert.Equal(new WeaponSlot(0), spawned.OwnerWeaponSlot);
        Assert.Equal(muzzlePosition, spawned.Position);
        Assert.Equal(fireVelocity, spawned.VelocityPerTick);
        Assert.Equal(spawned, outcome.UpdatedState.Projectiles[^1]);
    }

    [Fact]
    public void ResolveFire_WhenWeaponReady_UpdatesShooterLoadout()
    {
        MatchState state = CreateState(new SimTick(5));
        TankWeaponLoadout originalShooterLoadout = state.Loadouts[0];

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.NotSame(originalShooterLoadout, outcome.UpdatedState.Loadouts[0]);
        WeaponState updatedWeapon = outcome.UpdatedState.Loadouts[0].GetWeapon(new WeaponSlot(0));
        Assert.True(updatedWeapon.HasFired);
        Assert.Equal(new SimTick(5), updatedWeapon.LastFireTick);
    }

    [Fact]
    public void ResolveFire_WhenWeaponReady_PreservesArenaTickTanks()
    {
        MatchState state = CreateState(new SimTick(5));

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Same(state.Arena, outcome.UpdatedState.Arena);
        Assert.Equal(state.CurrentTick, outcome.UpdatedState.CurrentTick);
        Assert.Equal(state.Tanks.Count, outcome.UpdatedState.Tanks.Count);
        for (int i = 0; i < state.Tanks.Count; i++)
        {
            Assert.Equal(state.Tanks[i], outcome.UpdatedState.Tanks[i]);
        }
    }

    [Fact]
    public void ResolveFire_WhenWeaponReady_PreservesExistingProjectilesBeforeAppend()
    {
        ProjectileState existing = CreateProjectileForState(1);
        MatchState state = CreateState(
            new SimTick(5),
            projectiles: new[] { existing });

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Equal(2, outcome.UpdatedState.Projectiles.Count);
        Assert.Equal(existing, outcome.UpdatedState.Projectiles[0]);
        Assert.Equal(outcome.SpawnedProjectile!.Value, outcome.UpdatedState.Projectiles[1]);
    }

    [Fact]
    public void ResolveFire_WhenWeaponReady_PreservesNonShooterLoadoutReference()
    {
        MatchState state = CreateState(new SimTick(5));

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Same(state.Loadouts[1], outcome.UpdatedState.Loadouts[1]);
    }

    // -------------------- Not-ready fire --------------------

    [Fact]
    public void ResolveFire_WhenWeaponNotReady_ReturnsDidFireFalse()
    {
        TankWeaponLoadout firedLoadout = CreateFiredLoadout(new SimTick(5));
        MatchState state = CreateState(
            new SimTick(5),
            loadouts: new[] { firedLoadout, CreateReadyLoadout() });

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.False(outcome.DidFire);
        Assert.False(outcome.HasSpawnedProjectile);
        Assert.False(outcome.SpawnedProjectile.HasValue);
    }

    [Fact]
    public void ResolveFire_WhenWeaponNotReady_DoesNotAppendProjectile()
    {
        ProjectileState existing = CreateProjectileForState(1);
        TankWeaponLoadout firedLoadout = CreateFiredLoadout(new SimTick(5));
        MatchState state = CreateState(
            new SimTick(5),
            loadouts: new[] { firedLoadout, CreateReadyLoadout() },
            projectiles: new[] { existing });

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Equal(state.Projectiles.Count, outcome.UpdatedState.Projectiles.Count);
        Assert.Equal(existing, outcome.UpdatedState.Projectiles[0]);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void ResolveFire_DoesNotMutateOriginalState()
    {
        ProjectileState existing = CreateProjectileForState(1);
        MatchState state = CreateState(
            new SimTick(5),
            projectiles: new[] { existing });

        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        ProjectileState[] originalProjectiles = state.Projectiles.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];

        MatchStateFireSystem.ResolveFire(
            state,
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            currentTick: new SimTick(5),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTanks.Length, state.Tanks.Count);
        for (int i = 0; i < originalTanks.Length; i++)
        {
            Assert.Equal(originalTanks[i], state.Tanks[i]);
        }
        Assert.Equal(originalProjectiles.Length, state.Projectiles.Count);
        for (int i = 0; i < originalProjectiles.Length; i++)
        {
            Assert.Equal(originalProjectiles[i], state.Projectiles[i]);
        }
        Assert.Same(originalLoadout0, state.Loadouts[0]);
        Assert.Same(originalLoadout1, state.Loadouts[1]);
    }

    // -------------------- Request overload --------------------

    [Fact]
    public void ResolveFire_WithRequestOverload_WhenWeaponReady_AppendsProjectile()
    {
        MatchState state = CreateState(new SimTick(5));
        FixedVec2 muzzlePosition = FixedVec2.FromInts(10, 20);
        FixedVec2 fireVelocity = new FixedVec2(Fixed.FromInt(1), Fixed.Zero);
        MatchFireRequest request = new MatchFireRequest(
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            projectileId: new ProjectileId(123),
            muzzlePosition: muzzlePosition,
            fireVelocity: fireVelocity);

        MatchStateFireOutcome outcome = MatchStateFireSystem.ResolveFire(
            state,
            request,
            new SimTick(5));

        Assert.True(outcome.DidFire);
        Assert.True(outcome.HasSpawnedProjectile);
        ProjectileState spawned = outcome.SpawnedProjectile!.Value;
        Assert.Equal(new ProjectileId(123), spawned.Id);
        Assert.Equal(new TankId(0), spawned.OwnerTankId);
        Assert.Equal(new WeaponSlot(0), spawned.OwnerWeaponSlot);
        Assert.Equal(muzzlePosition, spawned.Position);
        Assert.Equal(fireVelocity, spawned.VelocityPerTick);
        Assert.Equal(spawned, outcome.UpdatedState.Projectiles[^1]);
    }

    [Fact]
    public void ResolveFire_WithRequestOverload_RejectsNullState()
    {
        MatchFireRequest request = new MatchFireRequest(
            shooterTankId: new TankId(0),
            weaponSlot: new WeaponSlot(0),
            projectileId: new ProjectileId(123),
            muzzlePosition: FixedVec2.FromInts(10, 20),
            fireVelocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        Assert.Throws<ArgumentNullException>(
            () => MatchStateFireSystem.ResolveFire(
                state: null!,
                request,
                new SimTick(5)));
    }
}
