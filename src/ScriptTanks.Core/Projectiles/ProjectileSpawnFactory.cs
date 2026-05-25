using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Deterministic helper for creating initial projectile runtime states.
/// The factory only constructs <see cref="ProjectileState"/> values:
/// <see cref="ProjectileState.RemainingRange"/> is initialized to
/// <see cref="ProjectileDefinition.MaxRange"/> and
/// <see cref="ProjectileState.IsActive"/> is set to <c>true</c>. All other
/// fields are passed through unchanged.
/// <para>
/// The factory does not validate weapon cooldowns, compute aim directions,
/// derive spawn points from tank geometry, perform hit detection, apply
/// damage, generate combat logs or replay frames, or mutate any match
/// state. Velocity is not required to match
/// <see cref="ProjectileDefinition.SpeedPerTick"/>; <see cref="FixedVec2.Zero"/>
/// velocity is permitted.
/// </para>
/// </summary>
public static class ProjectileSpawnFactory
{
    public static ProjectileState Create(
        ProjectileId projectileId,
        ProjectileDefinition definition,
        TankId ownerTankId,
        WeaponSlot ownerWeaponSlot,
        FixedVec2 position,
        FixedVec2 velocityPerTick,
        SimTick spawnTick)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new ProjectileState(
            id: projectileId,
            definition: definition,
            ownerTankId: ownerTankId,
            ownerWeaponSlot: ownerWeaponSlot,
            spawnTick: spawnTick,
            position: position,
            velocityPerTick: velocityPerTick,
            remainingRange: definition.MaxRange,
            isActive: true);
    }
}
