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

public sealed class MatchTickFireThenProjectilePipelineTests
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

    private static MatchState CreateState(
        SimTick tick,
        TankState[]? tanks = null,
        TankWeaponLoadout[]? loadouts = null,
        ProjectileState[]? projectiles = null)
    {
        TankState[] actualTanks = tanks ?? new[]
        {
            CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
            CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
        };
        TankWeaponLoadout[] actualLoadouts = loadouts
            ?? actualTanks.Select(_ => CreateReadyLoadout()).ToArray();

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            actualTanks,
            actualLoadouts,
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

    // -------------------- Outcome model --------------------

    [Fact]
    public void Outcome_RejectsNullUpdatedState()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchTickFireThenProjectileOutcome(
                updatedState: null!,
                fireOutcome: null,
                hadFireRequest: false));
    }

    [Fact]
    public void Outcome_RejectsFireRequestWithoutFireOutcome()
    {
        MatchState state = CreateState(new SimTick(5));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchTickFireThenProjectileOutcome(
                updatedState: state,
                fireOutcome: null,
                hadFireRequest: true));
        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Outcome_RejectsNoFireRequestWithFireOutcome()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchTickFireThenProjectileOutcome(
                updatedState: state,
                fireOutcome: fireOutcome,
                hadFireRequest: false));
        Assert.Equal("fireOutcome", ex.ParamName);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Step_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchTickFireThenProjectilePipeline.Step(
                state: null!,
                fireRequest: null));

        Assert.Throws<ArgumentNullException>(
            () => MatchTickFireThenProjectilePipeline.Step(
                state: null!,
                fireRequest: CreateRequest()));
    }

    // -------------------- No fire request --------------------

    [Fact]
    public void Step_WithoutFireRequest_BehavesLikeMatchTickPipeline()
    {
        MatchState state = CreateState(new SimTick(5));

        MatchTickFireThenProjectileOutcome outcome =
            MatchTickFireThenProjectilePipeline.Step(state, fireRequest: null);

        Assert.False(outcome.HadFireRequest);
        Assert.Null(outcome.FireOutcome);
        Assert.Equal(new SimTick(6), outcome.UpdatedState.CurrentTick);
        Assert.Empty(outcome.UpdatedState.Projectiles);
        Assert.Equal(state.Tanks.Count, outcome.UpdatedState.Tanks.Count);
        for (int i = 0; i < state.Tanks.Count; i++)
        {
            Assert.Equal(state.Tanks[i], outcome.UpdatedState.Tanks[i]);
        }

        MatchState directReference = MatchTickPipeline.Step(state);
        Assert.Equal(directReference.CurrentTick, outcome.UpdatedState.CurrentTick);
        Assert.Equal(directReference.Projectiles.Count, outcome.UpdatedState.Projectiles.Count);
    }

    // -------------------- Ready fire --------------------

    [Fact]
    public void Step_WithReadyFireRequest_FiresThenMovesProjectile()
    {
        MatchState state = CreateState(new SimTick(5));
        FixedVec2 muzzle = FixedVec2.FromInts(50, 50);
        FixedVec2 velocity = new FixedVec2(Fixed.FromInt(1), Fixed.Zero);
        MatchFireRequest request = CreateRequest(muzzle: muzzle, velocity: velocity);

        MatchTickFireThenProjectileOutcome outcome =
            MatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.True(outcome.HadFireRequest);
        Assert.NotNull(outcome.FireOutcome);
        Assert.True(outcome.FireOutcome!.DidFire);
        Assert.True(outcome.FireOutcome.SpawnedProjectile.HasValue);
        Assert.Equal(muzzle, outcome.FireOutcome.SpawnedProjectile!.Value.Position);

        Assert.Equal(new SimTick(6), outcome.UpdatedState.CurrentTick);
        Assert.Single(outcome.UpdatedState.Projectiles);
        Assert.Equal(FixedVec2.FromInts(51, 50), outcome.UpdatedState.Projectiles[0].Position);

        Fixed expectedRange =
            WeaponCatalog.StandardCannon.ProjectileRange
            - WeaponCatalog.StandardCannon.ProjectileSpeedPerTick;
        Assert.Equal(expectedRange, outcome.UpdatedState.Projectiles[0].RemainingRange);
    }

    [Fact]
    public void Step_WithReadyFireRequest_CanHitInSameTick()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20), hp: 100);
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20), hp: 100);
        MatchState state = CreateState(
            new SimTick(5),
            tanks: new[] { tank0, tank1 });

        MatchFireRequest request = CreateRequest(
            muzzle: FixedVec2.FromInts(87, 20),
            velocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        MatchTickFireThenProjectileOutcome outcome =
            MatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.NotNull(outcome.FireOutcome);
        Assert.True(outcome.FireOutcome!.DidFire);
        Assert.Equal(new SimTick(6), outcome.UpdatedState.CurrentTick);
        Assert.Empty(outcome.UpdatedState.Projectiles);

        TankState updatedTank1 = outcome.UpdatedState.Tanks
            .First(t => t.Id == new TankId(1));
        Assert.Equal(80, updatedTank1.CurrentHitPoints);
    }

    // -------------------- Not-ready fire --------------------

    [Fact]
    public void Step_WithNotReadyFireRequest_DoesNotSpawnProjectileButStillAdvancesTick()
    {
        TankWeaponLoadout firedLoadout = CreateFiredLoadout(new SimTick(5));
        MatchState state = CreateState(
            new SimTick(5),
            loadouts: new[] { firedLoadout, CreateReadyLoadout() });

        MatchFireRequest request = CreateRequest();

        MatchTickFireThenProjectileOutcome outcome =
            MatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.True(outcome.HadFireRequest);
        Assert.NotNull(outcome.FireOutcome);
        Assert.False(outcome.FireOutcome!.DidFire);
        Assert.False(outcome.FireOutcome.HasSpawnedProjectile);
        Assert.False(outcome.FireOutcome.SpawnedProjectile.HasValue);
        Assert.Empty(outcome.UpdatedState.Projectiles);
        Assert.Equal(new SimTick(6), outcome.UpdatedState.CurrentTick);
    }

    // -------------------- Fire-outcome state distinction --------------------

    [Fact]
    public void Step_WithFireRequest_PreservesFireOutcomeAsImmediatePostFireState()
    {
        MatchState state = CreateState(new SimTick(5));
        FixedVec2 muzzle = FixedVec2.FromInts(50, 50);
        MatchFireRequest request = CreateRequest(
            muzzle: muzzle,
            velocity: new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

        MatchTickFireThenProjectileOutcome outcome =
            MatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.NotNull(outcome.FireOutcome);
        Assert.Equal(new SimTick(5), outcome.FireOutcome!.UpdatedState.CurrentTick);
        Assert.Single(outcome.FireOutcome.UpdatedState.Projectiles);
        Assert.Equal(muzzle, outcome.FireOutcome.UpdatedState.Projectiles[0].Position);

        Assert.Equal(new SimTick(6), outcome.UpdatedState.CurrentTick);
        Assert.Single(outcome.UpdatedState.Projectiles);
        Assert.Equal(FixedVec2.FromInts(51, 50), outcome.UpdatedState.Projectiles[0].Position);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void Step_DoesNotMutateOriginalState()
    {
        ProjectileDefinition projectileDefinition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));
        ProjectileState existing = new ProjectileState(
            new ProjectileId(1),
            projectileDefinition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(0),
            FixedVec2.FromInts(50, 50),
            FixedVec2.FromInts(1, 0),
            projectileDefinition.MaxRange,
            isActive: true);
        MatchState state = CreateState(
            new SimTick(5),
            projectiles: new[] { existing });

        SimTick originalTick = state.CurrentTick;
        ArenaDefinition originalArena = state.Arena;
        TankState[] originalTanks = state.Tanks.ToArray();
        ProjectileState[] originalProjectiles = state.Projectiles.ToArray();
        TankWeaponLoadout originalLoadout0 = state.Loadouts[0];
        TankWeaponLoadout originalLoadout1 = state.Loadouts[1];

        MatchTickFireThenProjectilePipeline.Step(state, CreateRequest());

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
}
