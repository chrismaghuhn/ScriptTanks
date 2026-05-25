using System;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure factory for projectile lifecycle and damage-adjacent combat-log entries.
/// </summary>
/// <remarks>
/// This type only converts existing projectile/tank/hit data into immutable
/// combat-log data. It does not perform projectile movement, hit detection,
/// hit resolution, damage calculation, cleanup, pipeline integration, runner
/// integration, replay-recorder integration, serialization, UI, or
/// Godot-facing behavior. The supplied <see cref="SimTick"/> is used for every
/// generated entry. Messages are concrete human-readable strings built from
/// the supplied IDs - no template formatter is used or introduced here.
/// </remarks>
public static class ProjectileCombatLogFactory
{
    public static CombatLog CreateProjectileSpawnedLog(
        SimTick tick,
        ProjectileState projectile)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Projectile,
            CombatLogEventTypes.ProjectileSpawned,
            $"Projectile {projectile.Id} spawned from tank {projectile.OwnerTankId}.");

        return builder.Build();
    }

    public static CombatLog CreateProjectileHitLog(
        SimTick tick,
        ProjectileState projectile,
        ProjectileHitResult hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        string target = hit.Kind switch
        {
            ProjectileHitKind.Tank => $"tank {hit.TankId!.Value}",
            ProjectileHitKind.Wall => $"wall {hit.WallBlockId}",
            ProjectileHitKind.None => "nothing",
            _ => $"unknown target kind {hit.Kind}",
        };

        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Projectile,
            CombatLogEventTypes.ProjectileHit,
            $"Projectile {projectile.Id} hit {target}.");

        return builder.Build();
    }

    public static CombatLog CreateProjectileExpiredLog(
        SimTick tick,
        ProjectileState projectile)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Projectile,
            CombatLogEventTypes.ProjectileExpired,
            $"Projectile {projectile.Id} expired.");

        return builder.Build();
    }

    public static CombatLog CreateProjectileCleanedUpLog(
        SimTick tick,
        ProjectileState projectile)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Projectile,
            CombatLogEventTypes.ProjectileCleanedUp,
            $"Projectile {projectile.Id} was cleaned up.");

        return builder.Build();
    }

    public static CombatLog CreateDamageDealtLog(
        SimTick tick,
        TankState tank,
        int damage)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Damage,
            CombatLogEventTypes.DamageDealt,
            $"Tank {tank.Id} took {damage} damage.");

        return builder.Build();
    }

    public static CombatLog CreateTankDestroyedLog(
        SimTick tick,
        TankState tank)
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Tank,
            CombatLogEventTypes.TankDestroyed,
            $"Tank {tank.Id} was destroyed.");

        return builder.Build();
    }
}
