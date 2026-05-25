using System;
using System.Collections.Generic;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Deterministic pure resolver for projectile hit results. Translates a
/// <see cref="ProjectileState"/> and a <see cref="ProjectileHitResult"/>
/// into a <see cref="ProjectileHitResolutionOutcome"/>:
/// <list type="bullet">
///   <item><description>Inactive projectiles produce a no-op outcome regardless of <paramref name="hit"/>.</description></item>
///   <item><description><see cref="ProjectileHitKind.None"/> produces a no-op outcome.</description></item>
///   <item><description><see cref="ProjectileHitKind.Wall"/> deactivates the projectile; no tank update.</description></item>
///   <item><description><see cref="ProjectileHitKind.Tank"/> deactivates the projectile and applies projectile <c>RawDamage</c> to the first tank in <paramref name="tanks"/> whose <see cref="TankState.Id"/> matches <see cref="ProjectileHitResult.TankId"/> via <see cref="DamageApplication.Apply(TankState, int)"/>.</description></item>
/// </list>
/// The resolver does not mutate input collections or tanks, replace tanks in
/// any match-state list, generate combat logs or replay frames, raise
/// death/win events, or perform owner/team/destroyed filtering.
/// </summary>
public static class ProjectileHitResolution
{
    public static ProjectileHitResolutionOutcome Resolve(
        ProjectileState projectile,
        ProjectileHitResult hit,
        IEnumerable<TankState> tanks)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(tanks);

        if (!projectile.IsActive)
        {
            return new ProjectileHitResolutionOutcome(projectile, updatedTank: null);
        }

        if (hit.Kind == ProjectileHitKind.None)
        {
            return new ProjectileHitResolutionOutcome(projectile, updatedTank: null);
        }

        ProjectileState deactivatedProjectile = projectile.Deactivate();

        if (hit.Kind == ProjectileHitKind.Wall)
        {
            return new ProjectileHitResolutionOutcome(
                deactivatedProjectile,
                updatedTank: null);
        }

        if (hit.Kind == ProjectileHitKind.Tank)
        {
            foreach (TankState tank in tanks)
            {
                if (hit.TankId.HasValue && tank.Id.Equals(hit.TankId.Value))
                {
                    TankState updatedTank = DamageApplication.Apply(
                        tank,
                        projectile.Definition.RawDamage);

                    return new ProjectileHitResolutionOutcome(
                        deactivatedProjectile,
                        updatedTank);
                }
            }

            throw new InvalidOperationException(
                "Projectile tank hit result did not match any supplied tank.");
        }

        throw new InvalidOperationException(
            $"Unsupported projectile hit kind: {hit.Kind}.");
    }
}
