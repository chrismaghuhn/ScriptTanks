namespace ScriptTanks.Core.Math;

/// <summary>
/// Resolves a turn-fraction <see cref="Fixed"/> rotation to a deterministic forward
/// <see cref="FixedVec2"/> using a committed lookup table.
/// </summary>
/// <remarks>
/// Convention: one full turn equals <see cref="Fixed.One"/>; +X is east, +Y is north;
/// rotation increases counter-clockwise from +X. Normalization uses integer modulo on
/// <see cref="Fixed.Raw"/> with <see cref="Fixed.Scale"/> steps. Does not use runtime
/// trigonometry, mutate match state, resolve muzzle positions, or execute fire.
/// </remarks>
public static class FixedRotationDirectionResolver
{
    public static FixedRotationDirectionResult ResolveForward(Fixed rotation)
    {
        int index = (int)NormalizeRaw(rotation.Raw);
        return FixedRotationDirectionResult.Resolved(
            FixedRotationDirectionLookup.GetForward(index));
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
}
