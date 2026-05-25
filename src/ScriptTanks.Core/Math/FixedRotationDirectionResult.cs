using System;

namespace ScriptTanks.Core.Math;

/// <summary>
/// Represents the pure outcome of a future <c>FixedRotationDirectionResolver</c>.
/// </summary>
/// <remarks>
/// Immutable data carrier for whether a deterministic <see cref="FixedVec2"/> forward
/// direction could be derived from a <see cref="Fixed"/> rotation scalar. Does not read
/// lookup tables, normalize turn fractions, resolve muzzle positions, construct fire
/// requests, execute fire, or mutate match state.
/// </remarks>
public sealed class FixedRotationDirectionResult
{
    public FixedRotationDirectionStatus Status { get; }

    public FixedVec2? Forward { get; }

    public bool IsResolved =>
        Status == FixedRotationDirectionStatus.Resolved && Forward.HasValue;

    public FixedRotationDirectionResult(
        FixedRotationDirectionStatus status,
        FixedVec2? forward)
    {
        if (!Enum.IsDefined(typeof(FixedRotationDirectionStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Fixed rotation direction status must be a defined enum value.");
        }

        ValidateForwardInvariant(status, forward);

        Status = status;
        Forward = forward;
    }

    public static FixedRotationDirectionResult Resolved(FixedVec2 forward)
    {
        return new FixedRotationDirectionResult(
            FixedRotationDirectionStatus.Resolved,
            forward);
    }

    public static FixedRotationDirectionResult UnsupportedRotationConvention()
    {
        return new FixedRotationDirectionResult(
            FixedRotationDirectionStatus.UnsupportedRotationConvention,
            forward: null);
    }

    public static FixedRotationDirectionResult InvalidRotationValue()
    {
        return new FixedRotationDirectionResult(
            FixedRotationDirectionStatus.InvalidRotationValue,
            forward: null);
    }

    private static void ValidateForwardInvariant(
        FixedRotationDirectionStatus status,
        FixedVec2? forward)
    {
        if (status == FixedRotationDirectionStatus.Resolved)
        {
            if (!forward.HasValue)
            {
                throw new ArgumentException(
                    "Resolved status requires a non-null forward direction.",
                    nameof(forward));
            }
        }
        else if (forward.HasValue)
        {
            throw new ArgumentException(
                "Non-resolved statuses must not carry a forward direction.",
                nameof(forward));
        }
    }
}
