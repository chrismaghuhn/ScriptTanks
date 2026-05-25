using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure helper for determining whether a tank hitbox at a given center overlaps any
/// static <see cref="ArenaDefinition.WallBlocks"/> entry.
/// </summary>
/// <remarks>
/// Uses deterministic <see cref="ArenaDefinition.WallBlocks"/> order; returns the first
/// blocking <see cref="WallBlock.Id"/> when the tank circle intersects a wall rectangle
/// via <see cref="CollisionChecks.CircleIntersectsRect"/>. Performs no collision response,
/// movement rollback, pipeline orchestration, replay frames, or combat logging.
/// </remarks>
public static class TankWallOccupancyHelper
{
    /// <summary>
    /// Returns the <see cref="WallBlock.Id"/> of the first wall block whose bounds
    /// overlap the tank hitbox circle, or <see langword="null"/> when none overlap.
    /// </summary>
    /// <param name="arena">Arena whose <see cref="ArenaDefinition.WallBlocks"/> are checked in order.</param>
    /// <param name="tankCenter">Tank hitbox center in arena coordinates.</param>
    /// <param name="hitboxRadius">Strictly positive tank hitbox radius.</param>
    /// <returns>The first blocking wall id, or <see langword="null"/>.</returns>
    public static string? FindFirstBlockingWallBlockId(
        ArenaDefinition arena,
        FixedVec2 tankCenter,
        Fixed hitboxRadius)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ValidateHitboxRadius(hitboxRadius);

        CircleShape tankCircle = new CircleShape(tankCenter, hitboxRadius);

        foreach (WallBlock wall in arena.WallBlocks)
        {
            if (CollisionChecks.CircleIntersectsRect(tankCircle, wall.Bounds))
            {
                return wall.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns whether the tank hitbox at <paramref name="tankCenter"/> overlaps any
    /// static wall block in <paramref name="arena"/>.
    /// </summary>
    /// <param name="arena">Arena whose wall blocks are checked in order.</param>
    /// <param name="tankCenter">Tank hitbox center in arena coordinates.</param>
    /// <param name="hitboxRadius">Strictly positive tank hitbox radius.</param>
    /// <returns><see langword="true"/> when a blocking wall exists; otherwise <see langword="false"/>.</returns>
    public static bool IsBlocked(
        ArenaDefinition arena,
        FixedVec2 tankCenter,
        Fixed hitboxRadius)
        => FindFirstBlockingWallBlockId(arena, tankCenter, hitboxRadius) is not null;

    private static void ValidateHitboxRadius(Fixed hitboxRadius)
    {
        if (hitboxRadius <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hitboxRadius),
                hitboxRadius,
                "HitboxRadius must be positive.");
        }
    }
}
