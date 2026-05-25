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

public sealed class MatchStateTickAdvanceSystemTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null)
    {
        MovementState movement = new MovementState(
            position ?? FixedVec2.FromInts(10 + id, 20),
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

    private static ProjectileDefinition CreateProjectileDefinition()
        => new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

    private static ProjectileState CreateProjectile(int id)
    {
        ProjectileDefinition definition = CreateProjectileDefinition();

        return new ProjectileState(
            id: new ProjectileId(id),
            definition: definition,
            ownerTankId: new TankId(0),
            ownerWeaponSlot: new WeaponSlot(0),
            spawnTick: new SimTick(0),
            position: FixedVec2.FromInts(10 + id, 20),
            velocityPerTick: FixedVec2.FromInts(1, 0),
            remainingRange: definition.MaxRange,
            isActive: true);
    }

    private static MatchState CreateState()
        => new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            new[]
            {
                CreateTank(0, 0, FixedVec2.FromInts(10, 20)),
                CreateTank(1, 1, FixedVec2.FromInts(90, 20)),
            },
            new[] { CreateLoadout(), CreateLoadout() },
            new[] { CreateProjectile(1) });

    // -------------------- Validation --------------------

    [Fact]
    public void AdvanceTick_RejectsNullState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchStateTickAdvanceSystem.AdvanceTick(null!));
    }

    // -------------------- Tick --------------------

    [Fact]
    public void AdvanceTick_IncrementsCurrentTickByOne()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Equal(new SimTick(6), updated.CurrentTick);
    }

    // -------------------- Preservation --------------------

    [Fact]
    public void AdvanceTick_PreservesArenaReference()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Same(state.Arena, updated.Arena);
    }

    [Fact]
    public void AdvanceTick_PreservesTanks()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Equal(state.Tanks.Count, updated.Tanks.Count);
        Assert.Equal(state.Tanks[0], updated.Tanks[0]);
        Assert.Equal(state.Tanks[1], updated.Tanks[1]);
    }

    [Fact]
    public void AdvanceTick_PreservesLoadouts()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Equal(state.Loadouts.Count, updated.Loadouts.Count);
        Assert.Same(state.Loadouts[0], updated.Loadouts[0]);
        Assert.Same(state.Loadouts[1], updated.Loadouts[1]);
    }

    [Fact]
    public void AdvanceTick_PreservesProjectiles()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Equal(state.Projectiles.Count, updated.Projectiles.Count);
        Assert.Equal(state.Projectiles[0], updated.Projectiles[0]);
    }

    [Fact]
    public void AdvanceTick_ReturnsNewMatchStateInstance()
    {
        MatchState state = CreateState();

        MatchState updated = MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.NotSame(state, updated);
    }

    // -------------------- Purity --------------------

    [Fact]
    public void AdvanceTick_DoesNotMutateOriginalState()
    {
        MatchState state = CreateState();
        ArenaDefinition originalArena = state.Arena;
        SimTick originalTick = state.CurrentTick;
        TankState originalTank0 = state.Tanks[0];
        TankState originalTank1 = state.Tanks[1];
        ProjectileState originalProjectile = state.Projectiles[0];

        MatchStateTickAdvanceSystem.AdvanceTick(state);

        Assert.Equal(originalTick, state.CurrentTick);
        Assert.Same(originalArena, state.Arena);
        Assert.Equal(originalTank0, state.Tanks[0]);
        Assert.Equal(originalTank1, state.Tanks[1]);
        Assert.Equal(originalProjectile, state.Projectiles[0]);
    }
}
