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

public sealed class LoggedMatchStateFireSystemTests
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

    private static MatchFireRequest CreateRequest(
        int shooterId = 0,
        int projectileId = 123)
        => new MatchFireRequest(
            new TankId(shooterId),
            new WeaponSlot(0),
            new ProjectileId(projectileId),
            FixedVec2.FromInts(10, 20),
            new FixedVec2(Fixed.FromInt(1), Fixed.Zero));

    [Fact]
    public void LoggedMatchStateFireOutcome_RejectsNullFireOutcome()
    {
        CombatLog log = new CombatLogBuilder().Build();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchStateFireOutcome(null!, log));
        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void LoggedMatchStateFireOutcome_RejectsNullLog()
    {
        MatchStateFireOutcome fireOutcome =
            MatchStateFireOutcome.NotReady(CreateState(new SimTick(5)));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new LoggedMatchStateFireOutcome(fireOutcome, null!));
        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void ResolveFire_WhenWeaponReady_ReturnsFiredOutcomeAndLog()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchFireRequest request = CreateRequest();

        LoggedMatchStateFireOutcome outcome = LoggedMatchStateFireSystem.ResolveFire(
            state,
            request,
            new SimTick(5));

        Assert.True(outcome.FireOutcome.DidFire);
        Assert.True(outcome.FireOutcome.HasSpawnedProjectile);
        Assert.Equal(3, outcome.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, outcome.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireSucceeded, outcome.Log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.ProjectileSpawned, outcome.Log.Entries[2].EventType);
        Assert.Equal(
            new ProjectileId(123),
            outcome.FireOutcome.UpdatedState.Projectiles[^1].Id);
    }

    [Fact]
    public void ResolveFire_WhenWeaponNotReady_ReturnsNotReadyOutcomeAndLog()
    {
        MatchState state = CreateState(
            new SimTick(5),
            loadouts: new[]
            {
                CreateFiredLoadout(new SimTick(5)),
                CreateReadyLoadout(),
            });
        MatchFireRequest request = CreateRequest();

        LoggedMatchStateFireOutcome outcome = LoggedMatchStateFireSystem.ResolveFire(
            state,
            request,
            new SimTick(5));

        Assert.False(outcome.FireOutcome.DidFire);
        Assert.False(outcome.FireOutcome.HasSpawnedProjectile);
        Assert.Equal(2, outcome.Log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, outcome.Log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireNotReady, outcome.Log.Entries[1].EventType);
        Assert.Equal(
            state.Projectiles.Count,
            outcome.FireOutcome.UpdatedState.Projectiles.Count);
    }

    [Fact]
    public void ResolveFire_UsesCurrentTickForLogEntries()
    {
        MatchState state = CreateState(new SimTick(42));
        MatchFireRequest request = CreateRequest();

        LoggedMatchStateFireOutcome outcome = LoggedMatchStateFireSystem.ResolveFire(
            state,
            request,
            new SimTick(42));

        foreach (CombatLogEntry entry in outcome.Log.Entries)
        {
            Assert.Equal(new SimTick(42), entry.Tick);
        }
    }

    [Fact]
    public void ResolveFire_PropagatesNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => LoggedMatchStateFireSystem.ResolveFire(
                null!,
                CreateRequest(),
                new SimTick(5)));
    }

    [Fact]
    public void ResolveFire_PropagatesUnknownShooter()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchFireRequest request = CreateRequest(shooterId: 99);

        Assert.Throws<InvalidOperationException>(
            () => LoggedMatchStateFireSystem.ResolveFire(
                state,
                request,
                new SimTick(5)));
    }

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

        LoggedMatchStateFireSystem.ResolveFire(
            state,
            CreateRequest(),
            new SimTick(5));

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
