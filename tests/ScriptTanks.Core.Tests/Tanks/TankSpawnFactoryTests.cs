using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Tanks;

public sealed class TankSpawnFactoryTests
{
    private static ArenaStartPosition StartPosition()
        => new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 20));

    private static TankState CreateState()
        => TankSpawnFactory.Create(
            new TankId(7),
            TankCatalog.BasicTank,
            StartPosition());

    [Fact]
    public void Create_PreservesTankId()
    {
        TankState state = CreateState();
        Assert.Equal(new TankId(7), state.Id);
    }

    [Fact]
    public void Create_UsesStartPositionSlotAsOwnerSlot()
    {
        TankState state = CreateState();
        Assert.Equal(new PlayerSlot(1), state.OwnerSlot);
    }

    [Fact]
    public void Create_PreservesDefinitionReference()
    {
        TankState state = CreateState();
        Assert.Same(TankCatalog.BasicTank, state.Definition);
    }

    [Fact]
    public void Create_UsesStartPositionAsMovementPosition()
    {
        TankState state = CreateState();
        Assert.Equal(FixedVec2.FromInts(10, 20), state.Movement.Position);
    }

    [Fact]
    public void Create_SetsVelocityPerTickToZero()
    {
        TankState state = CreateState();
        Assert.Equal(FixedVec2.Zero, state.Movement.VelocityPerTick);
    }

    [Fact]
    public void Create_SetsCurrentHitPointsToDefinitionMaxHitPoints()
    {
        TankState state = CreateState();
        Assert.Equal(
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            state.CurrentHitPoints);
    }

    [Fact]
    public void Create_SetsBodyRotationToZero()
    {
        TankState state = CreateState();
        Assert.Equal(Fixed.Zero, state.BodyRotation);
    }

    [Fact]
    public void Create_SetsTurretRotationToZero()
    {
        TankState state = CreateState();
        Assert.Equal(Fixed.Zero, state.TurretRotation);
    }

    [Fact]
    public void Create_ReturnsNonDestroyedState()
    {
        TankState state = CreateState();
        Assert.False(state.IsDestroyed);
    }

    [Fact]
    public void Create_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => TankSpawnFactory.Create(
                new TankId(0),
                null!,
                StartPosition()));
    }

    [Fact]
    public void Create_FromArenaCatalogStartPosition_AndTankCatalogBasicTank_ProducesExpectedState()
    {
        ArenaDefinition arena = ArenaCatalog.OpenTestArena;
        ArenaStartPosition start = arena.StartPositions[0];

        TankState state = TankSpawnFactory.Create(
            new TankId(0),
            TankCatalog.BasicTank,
            start);

        Assert.Equal(start.Position, state.Movement.Position);
        Assert.Same(TankCatalog.BasicTank, state.Definition);
        Assert.Equal(
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            state.CurrentHitPoints);
        Assert.Equal(start.Slot, state.OwnerSlot);
    }
}
