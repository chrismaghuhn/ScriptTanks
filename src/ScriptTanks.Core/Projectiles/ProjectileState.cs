using System;
using System.Globalization;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Immutable runtime state for a projectile. Bundles identity
/// (<see cref="Id"/>, <see cref="Definition"/>), owner references
/// (<see cref="OwnerTankId"/>, <see cref="OwnerWeaponSlot"/>),
/// <see cref="SpawnTick"/> (match tick when spawned, for spawn-tick owner
/// filtering), kinematic state (<see cref="Position"/>,
/// <see cref="VelocityPerTick"/>), <see cref="RemainingRange"/>, and an
/// <see cref="IsActive"/> flag. The
/// struct does not move projectiles, detect hits, apply damage, generate
/// combat logs, or interact with match state.
/// <para>
/// <c>default(ProjectileState)</c> is permitted by the C# struct contract
/// but is NOT considered a valid gameplay state (e.g. <see cref="Definition"/>
/// would be <c>null</c>).
/// </para>
/// <para>
/// Equality uses <see cref="object.ReferenceEquals(object?, object?)"/> for
/// <see cref="Definition"/>, consistent with the deliberate absence of an
/// equality override on <see cref="ProjectileDefinition"/>. Two
/// <see cref="ProjectileState"/> values referencing different
/// <see cref="ProjectileDefinition"/> instances therefore compare as
/// unequal even if those definitions carry identical data.
/// </para>
/// </summary>
public readonly struct ProjectileState : IEquatable<ProjectileState>
{
    public ProjectileId Id { get; }

    public ProjectileDefinition Definition { get; }

    public TankId OwnerTankId { get; }

    public WeaponSlot OwnerWeaponSlot { get; }

    public SimTick SpawnTick { get; }

    public FixedVec2 Position { get; }

    public FixedVec2 VelocityPerTick { get; }

    public Fixed RemainingRange { get; }

    public bool IsActive { get; }

    public ProjectileState(
        ProjectileId id,
        ProjectileDefinition definition,
        TankId ownerTankId,
        WeaponSlot ownerWeaponSlot,
        SimTick spawnTick,
        FixedVec2 position,
        FixedVec2 velocityPerTick,
        Fixed remainingRange,
        bool isActive)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (remainingRange < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(remainingRange),
                "RemainingRange must not be negative.");
        }

        Id = id;
        Definition = definition;
        OwnerTankId = ownerTankId;
        OwnerWeaponSlot = ownerWeaponSlot;
        SpawnTick = spawnTick;
        Position = position;
        VelocityPerTick = velocityPerTick;
        RemainingRange = remainingRange;
        IsActive = isActive;
    }

    public ProjectileState WithPosition(FixedVec2 position)
        => new ProjectileState(
            Id,
            Definition,
            OwnerTankId,
            OwnerWeaponSlot,
            SpawnTick,
            position,
            VelocityPerTick,
            RemainingRange,
            IsActive);

    public ProjectileState WithVelocityPerTick(FixedVec2 velocityPerTick)
        => new ProjectileState(
            Id,
            Definition,
            OwnerTankId,
            OwnerWeaponSlot,
            SpawnTick,
            Position,
            velocityPerTick,
            RemainingRange,
            IsActive);

    public ProjectileState WithRemainingRange(Fixed remainingRange)
        => new ProjectileState(
            Id,
            Definition,
            OwnerTankId,
            OwnerWeaponSlot,
            SpawnTick,
            Position,
            VelocityPerTick,
            remainingRange,
            IsActive);

    public ProjectileState WithIsActive(bool isActive)
        => new ProjectileState(
            Id,
            Definition,
            OwnerTankId,
            OwnerWeaponSlot,
            SpawnTick,
            Position,
            VelocityPerTick,
            RemainingRange,
            isActive);

    public ProjectileState Deactivate() => WithIsActive(false);

    public bool Equals(ProjectileState other)
        => Id.Equals(other.Id)
           && ReferenceEquals(Definition, other.Definition)
           && OwnerTankId.Equals(other.OwnerTankId)
           && OwnerWeaponSlot.Equals(other.OwnerWeaponSlot)
           && SpawnTick.Equals(other.SpawnTick)
           && Position.Equals(other.Position)
           && VelocityPerTick.Equals(other.VelocityPerTick)
           && RemainingRange.Equals(other.RemainingRange)
           && IsActive == other.IsActive;

    public override bool Equals(object? obj) => obj is ProjectileState other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Id);
        hash.Add(Definition);
        hash.Add(OwnerTankId);
        hash.Add(OwnerWeaponSlot);
        hash.Add(SpawnTick);
        hash.Add(Position);
        hash.Add(VelocityPerTick);
        hash.Add(RemainingRange);
        hash.Add(IsActive);
        return hash.ToHashCode();
    }

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "ProjectileState(id={0}, ownerTank={1}, ownerSlot={2}, spawnTick={3}, isActive={4})",
            Id,
            OwnerTankId,
            OwnerWeaponSlot,
            SpawnTick,
            IsActive);
}
