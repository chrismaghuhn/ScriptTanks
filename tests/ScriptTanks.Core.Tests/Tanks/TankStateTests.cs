using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Tanks;

public sealed class TankStateTests
{
    private static MovementState DefaultMovement()
        => new MovementState(
            position: FixedVec2.FromInts(10, 20),
            velocityPerTick: FixedVec2.FromRaw(100, 0));

    private static TankState CreateValidState()
    {
        return new TankState(
            id: new TankId(7),
            ownerSlot: new PlayerSlot(1),
            definition: TankCatalog.BasicTank,
            movement: DefaultMovement(),
            currentHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankDefinition CreateBasicTankClone()
    {
        return new TankDefinition(
            id: TankCatalog.BasicTank.Id,
            displayName: TankCatalog.BasicTank.DisplayName,
            description: TankCatalog.BasicTank.Description,
            stats: TankCatalog.BasicTank.Stats,
            tags: TankCatalog.BasicTank.Tags);
    }

    [Fact]
    public void Constructor_PreservesId()
    {
        TankState state = CreateValidState();
        Assert.Equal(new TankId(7), state.Id);
    }

    [Fact]
    public void Constructor_PreservesOwnerSlot()
    {
        TankState state = CreateValidState();
        Assert.Equal(new PlayerSlot(1), state.OwnerSlot);
    }

    [Fact]
    public void Constructor_PreservesDefinition()
    {
        TankState state = CreateValidState();
        Assert.Same(TankCatalog.BasicTank, state.Definition);
    }

    [Fact]
    public void Constructor_PreservesMovement()
    {
        TankState state = CreateValidState();
        Assert.Equal(DefaultMovement(), state.Movement);
    }

    [Fact]
    public void Constructor_PreservesCurrentHitPoints()
    {
        TankState state = CreateValidState();
        Assert.Equal(TankCatalog.BasicTank.Stats.MaxHitPoints, state.CurrentHitPoints);
    }

    [Fact]
    public void Constructor_PreservesBodyRotation()
    {
        TankState state = new TankState(
            id: new TankId(7),
            ownerSlot: new PlayerSlot(1),
            definition: TankCatalog.BasicTank,
            movement: DefaultMovement(),
            currentHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.FromRatio(1, 4),
            turretRotation: Fixed.Zero);

        Assert.Equal(Fixed.FromRatio(1, 4), state.BodyRotation);
    }

    [Fact]
    public void Constructor_PreservesTurretRotation()
    {
        TankState state = new TankState(
            id: new TankId(7),
            ownerSlot: new PlayerSlot(1),
            definition: TankCatalog.BasicTank,
            movement: DefaultMovement(),
            currentHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.FromRatio(1, 8));

        Assert.Equal(Fixed.FromRatio(1, 8), state.TurretRotation);
    }

    [Fact]
    public void Constructor_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankState(
                id: new TankId(0),
                ownerSlot: new PlayerSlot(0),
                definition: null!,
                movement: DefaultMovement(),
                currentHitPoints: 1,
                bodyRotation: Fixed.Zero,
                turretRotation: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsNegativeCurrentHitPoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TankState(
                id: new TankId(0),
                ownerSlot: new PlayerSlot(0),
                definition: TankCatalog.BasicTank,
                movement: DefaultMovement(),
                currentHitPoints: -1,
                bodyRotation: Fixed.Zero,
                turretRotation: Fixed.Zero));
    }

    [Fact]
    public void Constructor_RejectsCurrentHitPointsAboveMax()
    {
        int aboveMax = TankCatalog.BasicTank.Stats.MaxHitPoints + 1;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TankState(
                id: new TankId(0),
                ownerSlot: new PlayerSlot(0),
                definition: TankCatalog.BasicTank,
                movement: DefaultMovement(),
                currentHitPoints: aboveMax,
                bodyRotation: Fixed.Zero,
                turretRotation: Fixed.Zero));
    }

    [Fact]
    public void Constructor_AcceptsCurrentHitPointsEqualsMax()
    {
        int max = TankCatalog.BasicTank.Stats.MaxHitPoints;

        TankState state = new TankState(
            id: new TankId(0),
            ownerSlot: new PlayerSlot(0),
            definition: TankCatalog.BasicTank,
            movement: DefaultMovement(),
            currentHitPoints: max,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);

        Assert.Equal(max, state.CurrentHitPoints);
    }

    [Fact]
    public void Constructor_AcceptsCurrentHitPointsZero()
    {
        TankState state = new TankState(
            id: new TankId(0),
            ownerSlot: new PlayerSlot(0),
            definition: TankCatalog.BasicTank,
            movement: DefaultMovement(),
            currentHitPoints: 0,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);

        Assert.Equal(0, state.CurrentHitPoints);
    }

    [Fact]
    public void IsDestroyed_IsFalse_WhenHpAboveZero()
    {
        TankState state = CreateValidState();
        Assert.False(state.IsDestroyed);
    }

    [Fact]
    public void IsDestroyed_IsTrue_WhenHpZero()
    {
        TankState state = CreateValidState().WithCurrentHitPoints(0);
        Assert.True(state.IsDestroyed);
    }

    [Fact]
    public void WithMovement_ChangesOnlyMovement()
    {
        TankState original = CreateValidState();
        MovementState newMovement = new MovementState(
            position: FixedVec2.FromInts(99, 99),
            velocityPerTick: FixedVec2.FromInts(1, 1));

        TankState changed = original.WithMovement(newMovement);

        Assert.Equal(newMovement, changed.Movement);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.OwnerSlot, changed.OwnerSlot);
        Assert.Same(original.Definition, changed.Definition);
        Assert.Equal(original.CurrentHitPoints, changed.CurrentHitPoints);
        Assert.Equal(original.BodyRotation, changed.BodyRotation);
        Assert.Equal(original.TurretRotation, changed.TurretRotation);
    }

    [Fact]
    public void WithCurrentHitPoints_ChangesOnlyCurrentHitPoints()
    {
        TankState original = CreateValidState();
        TankState changed = original.WithCurrentHitPoints(42);

        Assert.Equal(42, changed.CurrentHitPoints);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.OwnerSlot, changed.OwnerSlot);
        Assert.Same(original.Definition, changed.Definition);
        Assert.Equal(original.Movement, changed.Movement);
        Assert.Equal(original.BodyRotation, changed.BodyRotation);
        Assert.Equal(original.TurretRotation, changed.TurretRotation);
    }

    [Fact]
    public void WithCurrentHitPoints_Zero_ReturnsDestroyedState()
    {
        TankState destroyed = CreateValidState().WithCurrentHitPoints(0);

        Assert.Equal(0, destroyed.CurrentHitPoints);
        Assert.True(destroyed.IsDestroyed);
    }

    [Fact]
    public void WithCurrentHitPoints_RejectsNegative()
    {
        TankState original = CreateValidState();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => original.WithCurrentHitPoints(-1));
    }

    [Fact]
    public void WithCurrentHitPoints_RejectsAboveMax()
    {
        TankState original = CreateValidState();
        int aboveMax = original.Definition.Stats.MaxHitPoints + 1;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => original.WithCurrentHitPoints(aboveMax));
    }

    [Fact]
    public void WithBodyRotation_ChangesOnlyBodyRotation()
    {
        TankState original = CreateValidState();
        Fixed newRotation = Fixed.FromRatio(1, 3);

        TankState changed = original.WithBodyRotation(newRotation);

        Assert.Equal(newRotation, changed.BodyRotation);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.OwnerSlot, changed.OwnerSlot);
        Assert.Same(original.Definition, changed.Definition);
        Assert.Equal(original.Movement, changed.Movement);
        Assert.Equal(original.CurrentHitPoints, changed.CurrentHitPoints);
        Assert.Equal(original.TurretRotation, changed.TurretRotation);
    }

    [Fact]
    public void WithTurretRotation_ChangesOnlyTurretRotation()
    {
        TankState original = CreateValidState();
        Fixed newRotation = Fixed.FromRatio(1, 5);

        TankState changed = original.WithTurretRotation(newRotation);

        Assert.Equal(newRotation, changed.TurretRotation);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.OwnerSlot, changed.OwnerSlot);
        Assert.Same(original.Definition, changed.Definition);
        Assert.Equal(original.Movement, changed.Movement);
        Assert.Equal(original.CurrentHitPoints, changed.CurrentHitPoints);
        Assert.Equal(original.BodyRotation, changed.BodyRotation);
    }

    [Fact]
    public void WithRotations_ChangesBothRotations_PreservesRest()
    {
        TankState original = CreateValidState();
        Fixed newBody = Fixed.FromRatio(1, 7);
        Fixed newTurret = Fixed.FromRatio(2, 7);

        TankState changed = original.WithRotations(newBody, newTurret);

        Assert.Equal(newBody, changed.BodyRotation);
        Assert.Equal(newTurret, changed.TurretRotation);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.OwnerSlot, changed.OwnerSlot);
        Assert.Same(original.Definition, changed.Definition);
        Assert.Equal(original.Movement, changed.Movement);
        Assert.Equal(original.CurrentHitPoints, changed.CurrentHitPoints);
    }

    [Fact]
    public void Equals_TankState_ReturnsTrue_ForSameValues()
    {
        TankState a = CreateValidState();
        TankState b = CreateValidState();

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenIdDiffers()
    {
        TankState original = CreateValidState();
        TankState changed = new TankState(
            id: new TankId(8),
            ownerSlot: original.OwnerSlot,
            definition: original.Definition,
            movement: original.Movement,
            currentHitPoints: original.CurrentHitPoints,
            bodyRotation: original.BodyRotation,
            turretRotation: original.TurretRotation);

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOwnerSlotDiffers()
    {
        TankState original = CreateValidState();
        TankState changed = new TankState(
            id: original.Id,
            ownerSlot: new PlayerSlot(2),
            definition: original.Definition,
            movement: original.Movement,
            currentHitPoints: original.CurrentHitPoints,
            bodyRotation: original.BodyRotation,
            turretRotation: original.TurretRotation);

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDefinitionReferenceDiffers()
    {
        TankState original = CreateValidState();
        TankDefinition cloneDefinition = CreateBasicTankClone();
        TankState changed = new TankState(
            id: original.Id,
            ownerSlot: original.OwnerSlot,
            definition: cloneDefinition,
            movement: original.Movement,
            currentHitPoints: original.CurrentHitPoints,
            bodyRotation: original.BodyRotation,
            turretRotation: original.TurretRotation);

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMovementDiffers()
    {
        TankState original = CreateValidState();
        MovementState differentMovement = new MovementState(
            position: FixedVec2.FromInts(99, 99),
            velocityPerTick: FixedVec2.Zero);
        TankState changed = original.WithMovement(differentMovement);

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenCurrentHitPointsDiffer()
    {
        TankState original = CreateValidState();
        TankState changed = original.WithCurrentHitPoints(original.CurrentHitPoints - 1);

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenBodyRotationDiffers()
    {
        TankState original = CreateValidState();
        TankState changed = original.WithBodyRotation(Fixed.FromRatio(1, 11));

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenTurretRotationDiffers()
    {
        TankState original = CreateValidState();
        TankState changed = original.WithTurretRotation(Fixed.FromRatio(1, 13));

        Assert.False(original.Equals(changed));
    }

    [Fact]
    public void Equals_Object_HandlesNullAndWrongType()
    {
        TankState state = CreateValidState();

        Assert.False(state.Equals((object?)null));
        Assert.False(state.Equals("not a tank state"));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        TankState a = CreateValidState();
        TankState b = CreateValidState();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsKeyValues_SmokeOnly()
    {
        TankState state = CreateValidState();
        string text = state.ToString();

        Assert.Contains("7", text);
        Assert.Contains("1", text);
        Assert.Contains(TankCatalog.BasicTank.Id, text);
        Assert.Contains(
            TankCatalog.BasicTank.Stats.MaxHitPoints.ToString(System.Globalization.CultureInfo.InvariantCulture),
            text);
    }
}
