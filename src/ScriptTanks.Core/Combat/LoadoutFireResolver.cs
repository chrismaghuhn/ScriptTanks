using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Deterministic pure adapter for firing a weapon from a tank loadout. The
/// resolver reads the <see cref="WeaponState"/> at the requested
/// <see cref="WeaponSlot"/>, delegates cooldown handling and projectile
/// creation to <see cref="FireResolver"/>, and returns a new
/// <see cref="TankWeaponLoadout"/> with the updated weapon written back.
/// <para>
/// The resolver does not mutate the original loadout, projectile collections,
/// tank collections, match state, combat logs, or replay frames. It does not
/// compute aim direction, derive muzzle position, validate line-of-sight or
/// ammo, or check that <paramref name="fireVelocity"/> magnitude matches
/// <see cref="WeaponDefinition.ProjectileSpeedPerTick"/>.
/// </para>
/// </summary>
public static class LoadoutFireResolver
{
    public static LoadoutFireResolutionOutcome Resolve(
        TankState shooter,
        TankWeaponLoadout loadout,
        WeaponSlot weaponSlot,
        SimTick currentTick,
        ProjectileId projectileId,
        FixedVec2 muzzlePosition,
        FixedVec2 fireVelocity)
    {
        ArgumentNullException.ThrowIfNull(loadout);

        WeaponState weapon = loadout.GetWeapon(weaponSlot);

        FireResolutionOutcome fireOutcome = FireResolver.Resolve(
            shooter,
            weaponSlot,
            weapon,
            currentTick,
            projectileId,
            muzzlePosition,
            fireVelocity);

        TankWeaponLoadout updatedLoadout = loadout.WithWeapon(
            weaponSlot,
            fireOutcome.UpdatedWeapon);

        if (fireOutcome.DidFire)
        {
            return LoadoutFireResolutionOutcome.Fired(
                updatedLoadout,
                fireOutcome.SpawnedProjectile!.Value);
        }

        return LoadoutFireResolutionOutcome.NotReady(updatedLoadout);
    }
}
