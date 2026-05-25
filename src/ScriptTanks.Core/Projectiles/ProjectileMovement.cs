using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Deterministic one-tick projectile movement helper. This type only moves
/// projectile state and reduces remaining range. It does not perform hit
/// detection, collision resolution, damage application, combat logging, replay
/// recording, or match-level side effects.
/// <para>
/// Range is consumed by <see cref="ProjectileDefinition.SpeedPerTick"/>,
/// not by the magnitude of <see cref="ProjectileState.VelocityPerTick"/>.
/// This deliberately avoids any <c>Length()</c>/<c>Sqrt</c> dependency and
/// keeps the simulation deterministic and replay-stable.
/// </para>
/// </summary>
public static class ProjectileMovement
{
    /// <summary>
    /// Advances an active projectile by exactly one tick:
    /// <list type="bullet">
    ///   <item><description><c>Position += VelocityPerTick</c></description></item>
    ///   <item><description><c>RemainingRange -= Definition.SpeedPerTick</c></description></item>
    ///   <item><description>Deactivates and clamps <c>RemainingRange</c> to <see cref="Fixed.Zero"/> when it would reach or fall below zero.</description></item>
    /// </list>
    /// Inactive projectiles are returned unchanged.
    /// </summary>
    public static ProjectileState Step(ProjectileState projectile)
    {
        if (!projectile.IsActive)
        {
            return projectile;
        }

        Fixed remainingRange = projectile.RemainingRange - projectile.Definition.SpeedPerTick;
        bool isActive = remainingRange > Fixed.Zero;

        if (remainingRange < Fixed.Zero)
        {
            remainingRange = Fixed.Zero;
        }

        FixedVec2 position = projectile.Position + projectile.VelocityPerTick;

        return new ProjectileState(
            projectile.Id,
            projectile.Definition,
            projectile.OwnerTankId,
            projectile.OwnerWeaponSlot,
            projectile.SpawnTick,
            position,
            projectile.VelocityPerTick,
            remainingRange,
            isActive);
    }
}
