using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Immutable result of resolving a single projectile hit. Holds the updated
/// projectile and, for tank hits, the optional damage-updated
/// <see cref="TankState"/>. The class does not mutate match state, replace
/// tanks in any collection, create combat logs, produce replay frames,
/// determine winners, or raise events. The caller is responsible for
/// integrating <see cref="UpdatedTank"/> back into its match-state tank list.
/// </summary>
public sealed class ProjectileHitResolutionOutcome
{
    public ProjectileState UpdatedProjectile { get; }

    public TankState? UpdatedTank { get; }

    public bool HasUpdatedTank => UpdatedTank.HasValue;

    public ProjectileHitResolutionOutcome(
        ProjectileState updatedProjectile,
        TankState? updatedTank)
    {
        UpdatedProjectile = updatedProjectile;
        UpdatedTank = updatedTank;
    }
}
