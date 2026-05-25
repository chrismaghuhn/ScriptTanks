using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Deterministic pure projectile hit query against arena walls and a
/// caller-supplied list of candidate tanks. Wall blocks are checked in
/// <see cref="ArenaDefinition.WallBlocks"/> order before any tank, and the
/// first intersecting wall wins. Tanks are then checked in the order
/// produced by the supplied enumerable, and the first overlapping tank
/// wins.
/// <para>
/// The detector performs no owner/self-hit, team, or destroyed-tank
/// filtering. The caller is responsible for shaping the candidate tank
/// list (e.g. removing the projectile owner, removing destroyed tanks,
/// or restricting to enemies) before invoking
/// <see cref="Detect(ProjectileState, ArenaDefinition, IEnumerable{TankState})"/>.
/// </para>
/// <para>
/// This type does not apply damage, deactivate projectiles, mutate match
/// state, raise events, or generate combat logs or replay frames.
/// </para>
/// </summary>
public static class ProjectileHitDetection
{
    public static ProjectileHitResult Detect(
        ProjectileState projectile,
        ArenaDefinition arena,
        IEnumerable<TankState> tanks)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(tanks);

        if (!projectile.IsActive)
        {
            return ProjectileHitResult.None;
        }

        CircleShape projectileCircle = new CircleShape(
            projectile.Position,
            projectile.Definition.Radius);

        foreach (WallBlock wall in arena.WallBlocks)
        {
            if (CollisionChecks.CircleIntersectsRect(projectileCircle, wall.Bounds))
            {
                return ProjectileHitResult.Wall(wall);
            }
        }

        foreach (TankState tank in tanks)
        {
            CircleShape tankCircle = new CircleShape(
                tank.Movement.Position,
                tank.Definition.Stats.HitboxRadius);

            if (CollisionChecks.CirclesOverlap(projectileCircle, tankCircle))
            {
                return ProjectileHitResult.Tank(tank);
            }
        }

        return ProjectileHitResult.None;
    }
}
