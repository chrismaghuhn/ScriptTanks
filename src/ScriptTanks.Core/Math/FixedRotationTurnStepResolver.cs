using System;

namespace ScriptTanks.Core.Math;

/// <summary>
/// Resolves one clamped turn step from a current turn-fraction rotation toward a desired
/// rotation using a per-tick maximum turn rate.
/// </summary>
/// <remarks>
/// Uses shortest-path signed deltas in raw turn-fraction space with counter-clockwise
/// preference on exact half-turn ties. Normalization matches
/// <see cref="FixedRotationDirectionResolver"/>. Does not mutate match state, integrate
/// scripting pipelines, or use floating-point trigonometry.
/// </remarks>
public static class FixedRotationTurnStepResolver
{
    public static Fixed NormalizeRotation(Fixed rotation)
        => Fixed.FromRaw(NormalizeRaw(rotation.Raw));

    public static FixedRotationTurnStepResult ResolveStep(
        Fixed currentRotation,
        Fixed desiredRotation,
        Fixed maxTurnPerTick)
    {
        if (maxTurnPerTick < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTurnPerTick),
                maxTurnPerTick,
                "Max turn per tick must not be negative.");
        }

        long currentRaw = NormalizeRaw(currentRotation.Raw);
        long desiredRaw = NormalizeRaw(desiredRotation.Raw);

        Fixed normalizedCurrent = Fixed.FromRaw(currentRaw);
        Fixed normalizedDesired = Fixed.FromRaw(desiredRaw);

        if (currentRaw == desiredRaw)
        {
            return new FixedRotationTurnStepResult(
                normalizedCurrent,
                normalizedDesired,
                normalizedDesired,
                Fixed.Zero,
                Fixed.Zero,
                isAligned: true);
        }

        if (maxTurnPerTick == Fixed.Zero)
        {
            return new FixedRotationTurnStepResult(
                normalizedCurrent,
                normalizedDesired,
                normalizedCurrent,
                Fixed.Zero,
                Fixed.Zero,
                isAligned: false);
        }

        long shortestDelta = ResolveShortestSignedDeltaRaw(currentRaw, desiredRaw);
        long absDelta = AbsLong(shortestDelta);

        if (absDelta <= maxTurnPerTick.Raw)
        {
            return new FixedRotationTurnStepResult(
                normalizedCurrent,
                normalizedDesired,
                normalizedDesired,
                Fixed.FromRaw(shortestDelta),
                Fixed.FromRaw(absDelta),
                isAligned: true);
        }

        long stepRaw = shortestDelta > 0
            ? maxTurnPerTick.Raw
            : -maxTurnPerTick.Raw;

        long finalRaw = NormalizeRaw(currentRaw + stepRaw);

        return new FixedRotationTurnStepResult(
            normalizedCurrent,
            normalizedDesired,
            Fixed.FromRaw(finalRaw),
            Fixed.FromRaw(stepRaw),
            maxTurnPerTick,
            isAligned: false);
    }

    private static long ResolveShortestSignedDeltaRaw(long currentRaw, long desiredRaw)
    {
        const long scale = Fixed.Scale;
        long ccw = (desiredRaw - currentRaw + scale) % scale;

        long twiceCcw = ccw * 2;
        if (twiceCcw < scale)
        {
            return ccw;
        }

        if (twiceCcw > scale)
        {
            return ccw - scale;
        }

        return ccw;
    }

    private static long NormalizeRaw(long raw)
    {
        const long scale = Fixed.Scale;
        long normalized = raw % scale;
        if (normalized < 0)
        {
            normalized += scale;
        }

        return normalized;
    }

    private static long AbsLong(long value) => value < 0 ? -value : value;
}
