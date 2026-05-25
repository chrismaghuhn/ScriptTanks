namespace ScriptTanks.Core.Math;

/// <summary>
/// Inverse of <see cref="FixedRotationDirectionLookup"/>: maps a direction vector to the nearest
/// committed lookup index and corresponding <see cref="Fixed"/> turn-fraction rotation.
/// </summary>
/// <remarks>
/// Scores candidates with integer dot products (no floating-point math or runtime trigonometry).
/// On equal dot scores, the lower index wins. Cardinal axis-aligned directions use the same
/// fast-path indices as <see cref="FixedRotationAimResolver"/>. Does not mutate match state,
/// construct fire requests, or integrate scripting pipelines.
/// </remarks>
public static class FixedRotationInverseLookup
{
    /// <summary>
    /// Number of discrete forward lookup steps per full turn (matches the committed forward table).
    /// </summary>
    public static int StepCount => FixedRotationDirectionLookup.StepCount;

    /// <summary>
    /// Returns the turn-fraction rotation for a committed lookup index.
    /// </summary>
    /// <param name="index">Lookup index in <c>[0, <see cref="StepCount"/>)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative or not less than <see cref="StepCount"/>.
    /// </exception>
    public static Fixed RotationFromIndex(int index)
    {
        if (index < 0 || index >= StepCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Fixed.FromRaw(index);
    }

    /// <summary>
    /// Finds the lookup index whose forward direction has the largest dot product with
    /// <paramref name="direction"/>.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when <paramref name="direction"/> is zero; otherwise the best index.
    /// </returns>
    public static int? FindNearestIndex(FixedVec2 direction)
    {
        if (direction == FixedVec2.Zero)
        {
            return null;
        }

        int? cardinalIndex = TryGetCardinalIndex(direction);
        if (cardinalIndex.HasValue)
        {
            return cardinalIndex.Value;
        }

        int bestIndex = 0;
        Int128 bestScore = Score(direction, FixedRotationDirectionLookup.GetForward(0));

        for (int i = 1; i < StepCount; i++)
        {
            Int128 score = Score(direction, FixedRotationDirectionLookup.GetForward(i));
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Resolves <paramref name="direction"/> to a <see cref="FixedRotationAimResolution"/>.
    /// </summary>
    public static FixedRotationAimResolution ResolveFromDirection(FixedVec2 direction)
    {
        int? index = FindNearestIndex(direction);
        if (!index.HasValue)
        {
            return FixedRotationAimResolution.Unresolved();
        }

        return FixedRotationAimResolution.Resolved(RotationFromIndex(index.Value));
    }

    private static int? TryGetCardinalIndex(FixedVec2 direction)
    {
        if (direction.Y == Fixed.Zero)
        {
            if (direction.X > Fixed.Zero)
            {
                return 0;
            }

            if (direction.X < Fixed.Zero)
            {
                return StepCount / 2;
            }
        }

        if (direction.X == Fixed.Zero)
        {
            if (direction.Y > Fixed.Zero)
            {
                return StepCount / 4;
            }

            if (direction.Y < Fixed.Zero)
            {
                return StepCount * 3 / 4;
            }
        }

        return null;
    }

    private static Int128 Score(FixedVec2 direction, FixedVec2 forward) =>
        (Int128)direction.X.Raw * forward.X.Raw
        + (Int128)direction.Y.Raw * forward.Y.Raw;
}
