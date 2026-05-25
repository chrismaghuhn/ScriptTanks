using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Represents the pure outcome of a future <c>FireVelocityResolver</c>.
/// </summary>
/// <remarks>
/// Immutable data carrier for whether a deterministic <see cref="FixedVec2"/> fire
/// velocity could be derived. Does not read tanks, compute aim, scale speed, allocate
/// projectile ids, construct fire requests, execute fire, or mutate match state.
/// </remarks>
public sealed class FireVelocityResult
{
    public FireVelocityStatus Status { get; }

    public FixedVec2? FireVelocity { get; }

    public bool IsResolved =>
        Status == FireVelocityStatus.Resolved && FireVelocity.HasValue;

    public FireVelocityResult(
        FireVelocityStatus status,
        FixedVec2? fireVelocity)
    {
        if (!Enum.IsDefined(typeof(FireVelocityStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Fire velocity status must be a defined enum value.");
        }

        ValidateFireVelocityInvariant(status, fireVelocity);

        Status = status;
        FireVelocity = fireVelocity;
    }

    public static FireVelocityResult Resolved(FixedVec2 fireVelocity)
    {
        return new FireVelocityResult(
            FireVelocityStatus.Resolved,
            fireVelocity);
    }

    public static FireVelocityResult TankIndexOutOfRange()
    {
        return new FireVelocityResult(
            FireVelocityStatus.TankIndexOutOfRange,
            fireVelocity: null);
    }

    public static FireVelocityResult TankDestroyed()
    {
        return new FireVelocityResult(
            FireVelocityStatus.TankDestroyed,
            fireVelocity: null);
    }

    public static FireVelocityResult WeaponSlotMissing()
    {
        return new FireVelocityResult(
            FireVelocityStatus.WeaponSlotMissing,
            fireVelocity: null);
    }

    public static FireVelocityResult MissingAimDirection()
    {
        return new FireVelocityResult(
            FireVelocityStatus.MissingAimDirection,
            fireVelocity: null);
    }

    public static FireVelocityResult MissingWeaponSpeed()
    {
        return new FireVelocityResult(
            FireVelocityStatus.MissingWeaponSpeed,
            fireVelocity: null);
    }

    private static void ValidateFireVelocityInvariant(
        FireVelocityStatus status,
        FixedVec2? fireVelocity)
    {
        if (status == FireVelocityStatus.Resolved)
        {
            if (!fireVelocity.HasValue)
            {
                throw new ArgumentException(
                    "Resolved status requires a non-null fire velocity.",
                    nameof(fireVelocity));
            }
        }
        else if (fireVelocity.HasValue)
        {
            throw new ArgumentException(
                "Non-resolved statuses must not carry a fire velocity.",
                nameof(fireVelocity));
        }
    }
}
