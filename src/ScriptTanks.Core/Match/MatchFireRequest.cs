using System;
using System.Globalization;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable value object that groups the explicit fields needed to describe
/// one fire action: shooter tank id, weapon slot, projectile id, muzzle
/// position, and per-tick fire velocity.
/// </summary>
/// <remarks>
/// <para>
/// This type is a pure data carrier. It performs no validation beyond what
/// the wrapped value types already enforce
/// (<see cref="TankId"/>/<see cref="ProjectileId"/>/<see cref="WeaponSlot"/>
/// reject negative ints in their own constructors). It does not run gameplay
/// systems, mutate match state, allocate projectile ids, derive muzzle
/// positions or fire velocities, or queue intents.
/// </para>
/// <para>
/// Out of scope: AI / script execution, command queues, fire-intent
/// collection, tick-pipeline integration, match-runner integration,
/// projectile stepping / hit / cleanup, aiming or muzzle calculation,
/// projectile-id generation, fire-rate / friendly-fire / arena-bounds /
/// line-of-sight checks, combat logs, replay frames, serialization, and
/// Godot integration.
/// </para>
/// <para>
/// Equality is value-based: two requests are equal iff all five fields are
/// equal. <see cref="GetHashCode"/> is consistent with <see cref="Equals(MatchFireRequest)"/>.
/// </para>
/// </remarks>
public readonly struct MatchFireRequest : IEquatable<MatchFireRequest>
{
    public TankId ShooterTankId { get; }

    public WeaponSlot WeaponSlot { get; }

    public ProjectileId ProjectileId { get; }

    public FixedVec2 MuzzlePosition { get; }

    public FixedVec2 FireVelocity { get; }

    public MatchFireRequest(
        TankId shooterTankId,
        WeaponSlot weaponSlot,
        ProjectileId projectileId,
        FixedVec2 muzzlePosition,
        FixedVec2 fireVelocity)
    {
        ShooterTankId = shooterTankId;
        WeaponSlot = weaponSlot;
        ProjectileId = projectileId;
        MuzzlePosition = muzzlePosition;
        FireVelocity = fireVelocity;
    }

    public bool Equals(MatchFireRequest other)
        => ShooterTankId == other.ShooterTankId
           && WeaponSlot == other.WeaponSlot
           && ProjectileId == other.ProjectileId
           && MuzzlePosition == other.MuzzlePosition
           && FireVelocity == other.FireVelocity;

    public override bool Equals(object? obj)
        => obj is MatchFireRequest other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            ShooterTankId,
            WeaponSlot,
            ProjectileId,
            MuzzlePosition,
            FireVelocity);

    public static bool operator ==(MatchFireRequest left, MatchFireRequest right)
        => left.Equals(right);

    public static bool operator !=(MatchFireRequest left, MatchFireRequest right)
        => !left.Equals(right);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "MatchFireRequest(ShooterTankId={0}, WeaponSlot={1}, ProjectileId={2}, MuzzlePosition={3}, FireVelocity={4})",
            ShooterTankId,
            WeaponSlot,
            ProjectileId,
            MuzzlePosition,
            FireVelocity);
}
