using System;
using System.Collections.Generic;
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

public sealed class MatchStateTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        MovementState movement = new MovementState(
            FixedVec2.FromInts(10 + id, 20),
            FixedVec2.Zero);

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
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            new SimTick(5));
    }

    private static MatchState CreateState()
        => new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0), CreateTank(1, 1) },
            new[] { CreateLoadout(), CreateLoadout() },
            new[] { CreateProjectile(0) });

    // -------------------- Constructor Preservation --------------------

    [Fact]
    public void Constructor_PreservesArenaReference()
    {
        MatchState state = CreateState();

        Assert.Same(ArenaCatalog.OpenTestArena, state.Arena);
    }

    [Fact]
    public void Constructor_PreservesCurrentTick()
    {
        MatchState state = CreateState();

        Assert.Equal(new SimTick(5), state.CurrentTick);
    }

    [Fact]
    public void Constructor_PreservesTankCountAndOrder()
    {
        TankState tank0 = CreateTank(0, 0);
        TankState tank1 = CreateTank(1, 1);
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { tank0, tank1 },
            new[] { CreateLoadout(), CreateLoadout() },
            Array.Empty<ProjectileState>());

        Assert.Equal(2, state.Tanks.Count);
        Assert.Equal(tank0, state.Tanks[0]);
        Assert.Equal(tank1, state.Tanks[1]);
    }

    [Fact]
    public void Constructor_PreservesLoadoutCountAndOrder()
    {
        TankWeaponLoadout loadout0 = CreateLoadout();
        TankWeaponLoadout loadout1 = CreateLoadout();
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0), CreateTank(1, 1) },
            new[] { loadout0, loadout1 },
            Array.Empty<ProjectileState>());

        Assert.Equal(2, state.Loadouts.Count);
        Assert.Same(loadout0, state.Loadouts[0]);
        Assert.Same(loadout1, state.Loadouts[1]);
    }

    [Fact]
    public void Constructor_PreservesProjectileCountAndOrder()
    {
        ProjectileState projectile0 = CreateProjectile(0);
        ProjectileState projectile1 = CreateProjectile(1);
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0), CreateTank(1, 1) },
            new[] { CreateLoadout(), CreateLoadout() },
            new[] { projectile0, projectile1 });

        Assert.Equal(2, state.Projectiles.Count);
        Assert.Equal(projectile0, state.Projectiles[0]);
        Assert.Equal(projectile1, state.Projectiles[1]);
    }

    [Fact]
    public void Constructor_AcceptsEmptyProjectiles()
    {
        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0) },
            new[] { CreateLoadout() },
            Array.Empty<ProjectileState>());

        Assert.Empty(state.Projectiles);
    }

    // -------------------- Validation --------------------

    [Fact]
    public void Constructor_RejectsNullArena()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchState(
                arena: null!,
                currentTick: new SimTick(5),
                tanks: new[] { CreateTank(0, 0) },
                loadouts: new[] { CreateLoadout() },
                projectiles: Array.Empty<ProjectileState>()));
    }

    [Fact]
    public void Constructor_RejectsNullTanks()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchState(
                arena: ArenaCatalog.OpenTestArena,
                currentTick: new SimTick(5),
                tanks: null!,
                loadouts: new[] { CreateLoadout() },
                projectiles: Array.Empty<ProjectileState>()));
    }

    [Fact]
    public void Constructor_RejectsNullLoadouts()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchState(
                arena: ArenaCatalog.OpenTestArena,
                currentTick: new SimTick(5),
                tanks: new[] { CreateTank(0, 0) },
                loadouts: null!,
                projectiles: Array.Empty<ProjectileState>()));
    }

    [Fact]
    public void Constructor_RejectsNullProjectiles()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchState(
                arena: ArenaCatalog.OpenTestArena,
                currentTick: new SimTick(5),
                tanks: new[] { CreateTank(0, 0) },
                loadouts: new[] { CreateLoadout() },
                projectiles: null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyTanks()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchState(
                ArenaCatalog.OpenTestArena,
                new SimTick(5),
                Array.Empty<TankState>(),
                Array.Empty<TankWeaponLoadout>(),
                Array.Empty<ProjectileState>()));

        Assert.Equal("tanks", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsLoadoutCountBelowTankCount()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchState(
                ArenaCatalog.OpenTestArena,
                new SimTick(5),
                new[] { CreateTank(0, 0), CreateTank(1, 1) },
                new[] { CreateLoadout() },
                Array.Empty<ProjectileState>()));

        Assert.Equal("loadouts", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsLoadoutCountAboveTankCount()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new MatchState(
                ArenaCatalog.OpenTestArena,
                new SimTick(5),
                new[] { CreateTank(0, 0), CreateTank(1, 1) },
                new[] { CreateLoadout(), CreateLoadout(), CreateLoadout() },
                Array.Empty<ProjectileState>()));

        Assert.Equal("loadouts", ex.ParamName);
    }

    // -------------------- Defensive Copy --------------------

    [Fact]
    public void Constructor_DefensivelyCopiesTanks()
    {
        List<TankState> tanks = new List<TankState>
        {
            CreateTank(0, 0),
            CreateTank(1, 1),
        };
        List<TankWeaponLoadout> loadouts = new List<TankWeaponLoadout>
        {
            CreateLoadout(),
            CreateLoadout(),
        };

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            tanks,
            loadouts,
            Array.Empty<ProjectileState>());

        tanks.Add(CreateTank(2, 2));

        Assert.Equal(2, state.Tanks.Count);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesLoadouts()
    {
        List<TankWeaponLoadout> loadouts = new List<TankWeaponLoadout>
        {
            CreateLoadout(),
            CreateLoadout(),
        };

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0), CreateTank(1, 1) },
            loadouts,
            Array.Empty<ProjectileState>());

        loadouts.Add(CreateLoadout());

        Assert.Equal(2, state.Loadouts.Count);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesProjectiles()
    {
        List<ProjectileState> projectiles = new List<ProjectileState>();

        MatchState state = new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[] { CreateTank(0, 0) },
            new[] { CreateLoadout() },
            projectiles);

        projectiles.Add(CreateProjectile(0));

        Assert.Empty(state.Projectiles);
    }

    // -------------------- WithCurrentTick --------------------

    [Fact]
    public void WithCurrentTick_ChangesOnlyCurrentTick()
    {
        MatchState state = CreateState();

        MatchState updated = state.WithCurrentTick(new SimTick(42));

        Assert.Equal(new SimTick(42), updated.CurrentTick);
        Assert.Same(state.Arena, updated.Arena);
        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
        Assert.Equal(state.Projectiles.Count, updated.Projectiles.Count);
        Assert.Equal(state.Projectiles[0], updated.Projectiles[0]);
    }

    [Fact]
    public void WithCurrentTick_ReturnsNewInstance()
    {
        MatchState state = CreateState();

        MatchState updated = state.WithCurrentTick(new SimTick(42));

        Assert.NotSame(state, updated);
    }

    // -------------------- WithTanks --------------------

    [Fact]
    public void WithTanks_ReplacesTanks()
    {
        MatchState state = CreateState();
        TankState[] newTanks = new[]
        {
            CreateTank(10, 0),
            CreateTank(11, 1),
        };

        MatchState updated = state.WithTanks(newTanks);

        Assert.Equal(2, updated.Tanks.Count);
        Assert.Equal(new TankId(10), updated.Tanks[0].Id);
        Assert.Equal(new TankId(11), updated.Tanks[1].Id);
    }

    [Fact]
    public void WithTanks_PreservesArenaTickLoadoutsAndProjectiles()
    {
        MatchState state = CreateState();
        TankState[] newTanks = new[]
        {
            CreateTank(10, 0),
            CreateTank(11, 1),
        };

        MatchState updated = state.WithTanks(newTanks);

        Assert.Same(state.Arena, updated.Arena);
        Assert.Equal(state.CurrentTick, updated.CurrentTick);
        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
        Assert.Equal(state.Projectiles.Count, updated.Projectiles.Count);
        Assert.Equal(state.Projectiles[0], updated.Projectiles[0]);
    }

    [Fact]
    public void WithTanks_ValidatesNewTankList()
    {
        MatchState state = CreateState();

        Assert.Throws<ArgumentException>(
            () => state.WithTanks(Array.Empty<TankState>()));
    }

    // -------------------- WithLoadouts --------------------

    [Fact]
    public void WithLoadouts_ReplacesLoadouts()
    {
        MatchState state = CreateState();
        TankWeaponLoadout newLoadout0 = CreateLoadout();
        TankWeaponLoadout newLoadout1 = CreateLoadout();

        MatchState updated = state.WithLoadouts(new[] { newLoadout0, newLoadout1 });

        Assert.Equal(2, updated.Loadouts.Count);
        Assert.Same(newLoadout0, updated.Loadouts[0]);
        Assert.Same(newLoadout1, updated.Loadouts[1]);
    }

    [Fact]
    public void WithLoadouts_PreservesArenaTickTanksAndProjectiles()
    {
        MatchState state = CreateState();
        TankWeaponLoadout[] newLoadouts = new[] { CreateLoadout(), CreateLoadout() };

        MatchState updated = state.WithLoadouts(newLoadouts);

        Assert.Same(state.Arena, updated.Arena);
        Assert.Equal(state.CurrentTick, updated.CurrentTick);
        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
        Assert.Equal(state.Projectiles.Count, updated.Projectiles.Count);
        Assert.Equal(state.Projectiles[0], updated.Projectiles[0]);
    }

    [Fact]
    public void WithLoadouts_ValidatesLoadoutCount()
    {
        MatchState state = CreateState();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => state.WithLoadouts(new[] { CreateLoadout() }));

        Assert.Equal("loadouts", ex.ParamName);
    }

    // -------------------- WithProjectiles --------------------

    [Fact]
    public void WithProjectiles_ReplacesProjectiles()
    {
        MatchState state = CreateState();
        ProjectileState projectile0 = CreateProjectile(10);
        ProjectileState projectile1 = CreateProjectile(11);

        MatchState updated = state.WithProjectiles(new[] { projectile0, projectile1 });

        Assert.Equal(2, updated.Projectiles.Count);
        Assert.Equal(projectile0, updated.Projectiles[0]);
        Assert.Equal(projectile1, updated.Projectiles[1]);
    }

    [Fact]
    public void WithProjectiles_PreservesArenaTickTanksAndLoadouts()
    {
        MatchState state = CreateState();
        ProjectileState[] newProjectiles = new[] { CreateProjectile(10) };

        MatchState updated = state.WithProjectiles(newProjectiles);

        Assert.Same(state.Arena, updated.Arena);
        Assert.Equal(state.CurrentTick, updated.CurrentTick);
        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void WithProjectiles_AcceptsEmptyProjectiles()
    {
        MatchState state = CreateState();

        MatchState updated = state.WithProjectiles(Array.Empty<ProjectileState>());

        Assert.Empty(updated.Projectiles);
    }
}
