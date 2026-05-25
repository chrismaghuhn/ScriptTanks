using System;
using System.Globalization;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;

namespace ScriptTanks.Core.Tanks;

/// <summary>
/// Immutable runtime state for a spawned tank. Bundles identity
/// (<see cref="Id"/>, <see cref="OwnerSlot"/>), archetype
/// (<see cref="Definition"/>), kinematic state (<see cref="Movement"/>,
/// <see cref="BodyRotation"/>, <see cref="TurretRotation"/>), and combat
/// state (<see cref="CurrentHitPoints"/>). Valid gameplay states must be
/// created through the constructor; <c>default(TankState)</c> is permitted
/// by the C# struct contract but is NOT considered a valid gameplay state
/// (e.g. <see cref="Definition"/> would be <c>null</c>).
/// <para>
/// <see cref="BodyRotation"/> and <see cref="TurretRotation"/> are
/// deterministic scalar values without a fixed angle unit in this task and
/// are intentionally not normalized or range-checked here. The exact
/// interpretation is defined by the upcoming rotation system.
/// </para>
/// <para>
/// Equality uses <see cref="object.ReferenceEquals(object?, object?)"/>
/// for <see cref="Definition"/>, consistent with the deliberate absence of
/// an equality override on <see cref="TankDefinition"/>. Two
/// <see cref="TankState"/> values referencing different
/// <see cref="TankDefinition"/> instances therefore compare as unequal even
/// if those definitions carry identical data.
/// </para>
/// </summary>
public readonly struct TankState : IEquatable<TankState>
{
    public TankId Id { get; }

    public PlayerSlot OwnerSlot { get; }

    public TankDefinition Definition { get; }

    public MovementState Movement { get; }

    public int CurrentHitPoints { get; }

    public Fixed BodyRotation { get; }

    public Fixed TurretRotation { get; }

    public bool IsDestroyed => CurrentHitPoints <= 0;

    public TankState(
        TankId id,
        PlayerSlot ownerSlot,
        TankDefinition definition,
        MovementState movement,
        int currentHitPoints,
        Fixed bodyRotation,
        Fixed turretRotation)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (currentHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentHitPoints),
                currentHitPoints,
                "CurrentHitPoints must not be negative.");
        }

        if (currentHitPoints > definition.Stats.MaxHitPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentHitPoints),
                currentHitPoints,
                "CurrentHitPoints must not exceed Definition.Stats.MaxHitPoints.");
        }

        Id = id;
        OwnerSlot = ownerSlot;
        Definition = definition;
        Movement = movement;
        CurrentHitPoints = currentHitPoints;
        BodyRotation = bodyRotation;
        TurretRotation = turretRotation;
    }

    public TankState WithMovement(MovementState movement)
        => new TankState(
            Id,
            OwnerSlot,
            Definition,
            movement,
            CurrentHitPoints,
            BodyRotation,
            TurretRotation);

    public TankState WithCurrentHitPoints(int currentHitPoints)
        => new TankState(
            Id,
            OwnerSlot,
            Definition,
            Movement,
            currentHitPoints,
            BodyRotation,
            TurretRotation);

    public TankState WithBodyRotation(Fixed bodyRotation)
        => new TankState(
            Id,
            OwnerSlot,
            Definition,
            Movement,
            CurrentHitPoints,
            bodyRotation,
            TurretRotation);

    public TankState WithTurretRotation(Fixed turretRotation)
        => new TankState(
            Id,
            OwnerSlot,
            Definition,
            Movement,
            CurrentHitPoints,
            BodyRotation,
            turretRotation);

    public TankState WithRotations(Fixed bodyRotation, Fixed turretRotation)
        => new TankState(
            Id,
            OwnerSlot,
            Definition,
            Movement,
            CurrentHitPoints,
            bodyRotation,
            turretRotation);

    public bool Equals(TankState other)
        => Id.Equals(other.Id)
           && OwnerSlot.Equals(other.OwnerSlot)
           && ReferenceEquals(Definition, other.Definition)
           && Movement.Equals(other.Movement)
           && CurrentHitPoints == other.CurrentHitPoints
           && BodyRotation.Equals(other.BodyRotation)
           && TurretRotation.Equals(other.TurretRotation);

    public override bool Equals(object? obj) => obj is TankState other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            Id,
            OwnerSlot,
            Definition,
            Movement,
            CurrentHitPoints,
            BodyRotation,
            TurretRotation);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "TankState(id={0}, owner={1}, def={2}, hp={3})",
            Id,
            OwnerSlot,
            Definition.Id,
            CurrentHitPoints);
}
