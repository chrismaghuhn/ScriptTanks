using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Deterministic pure resolver for a single weapon fire attempt. The
/// resolver only checks weapon cooldown readiness via
/// <see cref="WeaponState.IsReady(SimTick)"/>, builds a
/// <see cref="ProjectileDefinition"/> from the weapon's
/// <see cref="WeaponDefinition"/>, spawns an initial
/// <see cref="ProjectileState"/> via <see cref="ProjectileSpawnFactory"/>,
/// and returns an updated weapon state via
/// <see cref="WeaponState.MarkFired(SimTick)"/>.
/// <para>
/// The resolver does not mutate loadouts, projectile collections, tank
/// state, match state, combat logs, or replay frames. It does not compute
/// aim direction, derive muzzle position from tank position/rotation,
/// validate line-of-sight or ammo, or check that
/// <paramref name="fireVelocity"/> magnitude matches
/// <see cref="WeaponDefinition.ProjectileSpeedPerTick"/>. Those concerns
/// belong to higher-level callers (e.g. a future MatchRunner).
/// </para>
/// </summary>
public static class FireResolver
{
    public static FireResolutionOutcome Resolve(
        TankState shooter,
        WeaponSlot weaponSlot,
        WeaponState weapon,
        SimTick currentTick,
        ProjectileId projectileId,
        FixedVec2 muzzlePosition,
        FixedVec2 fireVelocity)
    {
        if (!weapon.IsReady(currentTick))
        {
            return FireResolutionOutcome.NotReady(weapon);
        }

        ProjectileDefinition projectileDefinition = new ProjectileDefinition(
            rawDamage: weapon.Definition.RawDamage,
            speedPerTick: weapon.Definition.ProjectileSpeedPerTick,
            maxRange: weapon.Definition.ProjectileRange,
            radius: weapon.Definition.ProjectileRadius);

        ProjectileState projectile = ProjectileSpawnFactory.Create(
            projectileId,
            projectileDefinition,
            shooter.Id,
            weaponSlot,
            muzzlePosition,
            fireVelocity,
            currentTick);

        WeaponState updatedWeapon = weapon.MarkFired(currentTick);

        return FireResolutionOutcome.Fired(updatedWeapon, projectile);
    }
}
