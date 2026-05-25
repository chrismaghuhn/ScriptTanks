using System;
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

namespace ScriptTanks.Core.Tests.Logging;

public sealed class FireCombatLogFactoryTests
{
    private static MatchFireRequest CreateRequest()
        => new MatchFireRequest(
            new TankId(0),
            new WeaponSlot(0),
            new ProjectileId(123),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0));

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position)
    {
        MovementState movement = new MovementState(position, FixedVec2.Zero);

        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            movement,
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static MatchState CreateState()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 20));
        TankState tank1 = CreateTank(1, 1, FixedVec2.FromInts(90, 20));

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { tank0, tank1 },
            new[] { CreateLoadout(), CreateLoadout() },
            Array.Empty<ProjectileState>());
    }

    private static ProjectileDefinition CreateProjectileDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile()
    {
        ProjectileDefinition definition = CreateProjectileDefinition();

        return new ProjectileState(
            new ProjectileId(123),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(0),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            definition.MaxRange,
            isActive: true);
    }

    private static MatchStateFireOutcome CreateFiredOutcome()
        => MatchStateFireOutcome.Fired(CreateState(), CreateProjectile());

    private static MatchStateFireOutcome CreateNotReadyOutcome()
        => MatchStateFireOutcome.NotReady(CreateState());

    [Fact]
    public void CreateFireLog_WhenFired_ReturnsRequestedSucceededAndProjectileSpawnedEntries()
    {
        SimTick tick = new SimTick(5);
        MatchFireRequest request = CreateRequest();
        MatchStateFireOutcome outcome = CreateFiredOutcome();

        CombatLog log = FireCombatLogFactory.CreateFireLog(tick, request, outcome);

        Assert.Equal(3, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireSucceeded, log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.ProjectileSpawned, log.Entries[2].EventType);

        Assert.Equal(
            "Tank 0 requested fire from weapon slot 0.",
            log.Entries[0].Message);
        Assert.Equal(
            "Tank 0 fired weapon slot 0.",
            log.Entries[1].Message);
        Assert.Equal(
            "Projectile 123 spawned from tank 0.",
            log.Entries[2].Message);
    }

    [Fact]
    public void CreateFireLog_WhenNotReady_ReturnsRequestedAndNotReadyEntries()
    {
        SimTick tick = new SimTick(5);
        MatchFireRequest request = CreateRequest();
        MatchStateFireOutcome outcome = CreateNotReadyOutcome();

        CombatLog log = FireCombatLogFactory.CreateFireLog(tick, request, outcome);

        Assert.Equal(2, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.FireRequested, log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireNotReady, log.Entries[1].EventType);
        Assert.Equal(
            "Tank 0 could not fire weapon slot 0: weapon not ready.",
            log.Entries[1].Message);
    }

    [Fact]
    public void CreateFireLog_UsesProvidedTickForAllEntries()
    {
        SimTick tick = new SimTick(42);
        MatchFireRequest request = CreateRequest();
        MatchStateFireOutcome outcome = CreateFiredOutcome();

        CombatLog log = FireCombatLogFactory.CreateFireLog(tick, request, outcome);

        foreach (CombatLogEntry entry in log.Entries)
        {
            Assert.Equal(new SimTick(42), entry.Tick);
        }
    }

    [Fact]
    public void CreateFireLog_UsesExpectedCategoriesAndEventTypes()
    {
        SimTick tick = new SimTick(5);
        MatchFireRequest request = CreateRequest();
        MatchStateFireOutcome outcome = CreateFiredOutcome();

        CombatLog log = FireCombatLogFactory.CreateFireLog(tick, request, outcome);

        Assert.Equal(CombatLogCategory.Weapon, log.Entries[0].Category);
        Assert.Equal(CombatLogEventTypes.FireRequested, log.Entries[0].EventType);

        Assert.Equal(CombatLogCategory.Weapon, log.Entries[1].Category);
        Assert.Equal(CombatLogEventTypes.FireSucceeded, log.Entries[1].EventType);

        Assert.Equal(CombatLogCategory.Projectile, log.Entries[2].Category);
        Assert.Equal(CombatLogEventTypes.ProjectileSpawned, log.Entries[2].EventType);
    }

    [Fact]
    public void CreateFireLog_PreservesNonDecreasingOrder()
    {
        SimTick tick = new SimTick(5);
        MatchFireRequest request = CreateRequest();
        MatchStateFireOutcome outcome = CreateFiredOutcome();

        CombatLog log = FireCombatLogFactory.CreateFireLog(tick, request, outcome);

        Assert.Equal(3, log.Entries.Count);
        foreach (CombatLogEntry entry in log.Entries)
        {
            Assert.Equal(new SimTick(5), entry.Tick);
        }
    }

    [Fact]
    public void CreateFireLog_RejectsNullOutcome()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => FireCombatLogFactory.CreateFireLog(
                new SimTick(5),
                CreateRequest(),
                null!));
        Assert.Equal("outcome", ex.ParamName);
    }
}
