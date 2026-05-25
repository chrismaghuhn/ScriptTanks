using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class LoggedMatchTickFireThenProjectilePipelineTests
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

    [Fact]
    public void LoggedOutcome_RejectsNullTickOutcome()
    {
        CombatLog log = new CombatLogBuilder().Build();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchTickFireThenProjectileOutcome(null!, log));
        Assert.Equal("tickOutcome", ex.ParamName);
    }

    [Fact]
    public void LoggedOutcome_RejectsNullLog()
    {
        MatchTickFireThenProjectileOutcome tickOutcome =
            new MatchTickFireThenProjectileOutcome(
                updatedState: CreateState(new SimTick(5)),
                fireOutcome: null,
                hadFireRequest: false);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchTickFireThenProjectileOutcome(tickOutcome, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void Step_WithoutFireRequest_ReturnsEmptyLogAndAdvancesTick()
    {
        MatchState state = CreateState(new SimTick(5));

        LoggedMatchTickFireThenProjectileOutcome outcome =
            LoggedMatchTickFireThenProjectilePipeline.Step(state, fireRequest: null);

        Assert.False(outcome.TickOutcome.HadFireRequest);
        Assert.Null(outcome.TickOutcome.FireOutcome);
        Assert.Empty(outcome.Log.Entries);
        Assert.Equal(new SimTick(6), outcome.TickOutcome.UpdatedState.CurrentTick);
    }

    [Fact]
    public void Step_WithReadyFireRequest_ReturnsFireLogAndFinalTickOutcome()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchFireRequest request = CreateRequest();

        LoggedMatchTickFireThenProjectileOutcome outcome =
            LoggedMatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.True(outcome.TickOutcome.HadFireRequest);
        Assert.NotNull(outcome.TickOutcome.FireOutcome);
        Assert.True(outcome.TickOutcome.FireOutcome!.DidFire);
        Assert.Equal(3, outcome.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, outcome.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireSucceeded, outcome.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.ProjectileSpawned, outcome.Log.Entries[2].EventType);
        Assert.Equal(new SimTick(6), outcome.TickOutcome.UpdatedState.CurrentTick);
    }

    [Fact]
    public void Step_WithReadyFireRequest_CanMoveSpawnedProjectileSameTick()
    {
        MatchState state = CreateState(new SimTick(5));
        FixedVec2 muzzle = FixedVec2.FromInts(50, 50);
        FixedVec2 velocity = new FixedVec2(Fixed.FromInt(1), Fixed.Zero);
        MatchFireRequest request = CreateRequest(muzzle: muzzle, velocity: velocity);

        LoggedMatchTickFireThenProjectileOutcome outcome =
            LoggedMatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.NotNull(outcome.TickOutcome.FireOutcome);
        Assert.Equal(
            FixedVec2.FromInts(50, 50),
            outcome.TickOutcome.FireOutcome!.UpdatedState.Projectiles[0].Position);
        Assert.Equal(
            FixedVec2.FromInts(51, 50),
            outcome.TickOutcome.UpdatedState.Projectiles[0].Position);
        Assert.Equal(new SimTick(6), outcome.TickOutcome.UpdatedState.CurrentTick);
    }

    [Fact]
    public void Step_WithNotReadyFireRequest_ReturnsNotReadyLogAndAdvancesTick()
    {
        TankWeaponLoadout firedLoadout = CreateFiredLoadout(new SimTick(5));
        MatchState state = CreateState(
            new SimTick(5),
            loadouts: new[] { firedLoadout, CreateReadyLoadout() });
        MatchFireRequest request = CreateRequest();

        LoggedMatchTickFireThenProjectileOutcome outcome =
            LoggedMatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.True(outcome.TickOutcome.HadFireRequest);
        Assert.NotNull(outcome.TickOutcome.FireOutcome);
        Assert.False(outcome.TickOutcome.FireOutcome!.DidFire);
        Assert.Equal(2, outcome.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, outcome.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireNotReady, outcome.Log.Entries[1].EventType);
        Assert.Empty(outcome.TickOutcome.UpdatedState.Projectiles);
        Assert.Equal(new SimTick(6), outcome.TickOutcome.UpdatedState.CurrentTick);
    }

    [Fact]
    public void Step_UsesStateCurrentTickForLogEntries()
    {
        MatchState state = CreateState(new SimTick(42));
        MatchFireRequest request = CreateRequest();

        LoggedMatchTickFireThenProjectileOutcome outcome =
            LoggedMatchTickFireThenProjectilePipeline.Step(state, request);

        Assert.Equal(new SimTick(43), outcome.TickOutcome.UpdatedState.CurrentTick);
        foreach (CombatLogEntry entry in outcome.Log.Entries)
        {
            Assert.Equal(new SimTick(42), entry.Tick);
        }
    }

    [Fact]
    public void Step_PropagatesNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoggedMatchTickFireThenProjectilePipeline.Step(
                null!,
                CreateRequest()));
    }

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

        LoggedMatchTickFireThenProjectilePipeline.Step(state, CreateRequest());

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
