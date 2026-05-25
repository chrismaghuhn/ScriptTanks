using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Represents the pure outcome of a future <c>FireMuzzlePositionResolver</c>.
/// </summary>
/// <remarks>
/// Immutable data carrier for whether a deterministic <see cref="FixedVec2"/> muzzle
/// position could be derived. Does not read tanks, compute aim, allocate projectile ids,
/// construct fire requests, execute fire, or mutate match state.
/// </remarks>
public sealed class FireMuzzlePositionResult
{
    public FireMuzzlePositionStatus Status { get; }

    public FixedVec2? MuzzlePosition { get; }

    public bool IsResolved =>
        Status == FireMuzzlePositionStatus.Resolved && MuzzlePosition.HasValue;

    public FireMuzzlePositionResult(
        FireMuzzlePositionStatus status,
        FixedVec2? muzzlePosition)
    {
        if (!Enum.IsDefined(typeof(FireMuzzlePositionStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Fire muzzle position status must be a defined enum value.");
        }

        ValidateMuzzlePositionInvariant(status, muzzlePosition);

        Status = status;
        MuzzlePosition = muzzlePosition;
    }

    public static FireMuzzlePositionResult Resolved(FixedVec2 muzzlePosition)
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.Resolved,
            muzzlePosition);
    }

    public static FireMuzzlePositionResult TankIndexOutOfRange()
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.TankIndexOutOfRange,
            muzzlePosition: null);
    }

    public static FireMuzzlePositionResult TankDestroyed()
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.TankDestroyed,
            muzzlePosition: null);
    }

    public static FireMuzzlePositionResult WeaponSlotMissing()
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.WeaponSlotMissing,
            muzzlePosition: null);
    }

    public static FireMuzzlePositionResult MissingAimDirection()
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.MissingAimDirection,
            muzzlePosition: null);
    }

    public static FireMuzzlePositionResult MissingWeaponGeometry()
    {
        return new FireMuzzlePositionResult(
            FireMuzzlePositionStatus.MissingWeaponGeometry,
            muzzlePosition: null);
    }

    private static void ValidateMuzzlePositionInvariant(
        FireMuzzlePositionStatus status,
        FixedVec2? muzzlePosition)
    {
        if (status == FireMuzzlePositionStatus.Resolved)
        {
            if (!muzzlePosition.HasValue)
            {
                throw new ArgumentException(
                    "Resolved status requires a non-null muzzle position.",
                    nameof(muzzlePosition));
            }
        }
        else if (muzzlePosition.HasValue)
        {
            throw new ArgumentException(
                "Non-resolved statuses must not carry a muzzle position.",
                nameof(muzzlePosition));
        }
    }
}
