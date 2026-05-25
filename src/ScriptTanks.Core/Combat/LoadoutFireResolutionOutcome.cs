using System;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Immutable result of resolving a fire attempt from a tank weapon loadout.
/// Holds the updated loadout and, if a shot was fired, the spawned
/// projectile. The class does not mutate match state, projectile collections,
/// tank collections, combat logs, or replay frames.
/// <para>
/// Construction is restricted to the named factory methods
/// <see cref="NotReady(TankWeaponLoadout)"/> and
/// <see cref="Fired(TankWeaponLoadout, ProjectileState)"/> to keep the
/// (DidFire, SpawnedProjectile) invariant consistent.
/// </para>
/// </summary>
public sealed class LoadoutFireResolutionOutcome
{
    public bool DidFire { get; }

    public TankWeaponLoadout UpdatedLoadout { get; }

    public ProjectileState? SpawnedProjectile { get; }

    private LoadoutFireResolutionOutcome(
        bool didFire,
        TankWeaponLoadout updatedLoadout,
        ProjectileState? spawnedProjectile)
    {
        DidFire = didFire;
        UpdatedLoadout = updatedLoadout;
        SpawnedProjectile = spawnedProjectile;
    }

    public static LoadoutFireResolutionOutcome NotReady(TankWeaponLoadout updatedLoadout)
    {
        ArgumentNullException.ThrowIfNull(updatedLoadout);

        return new LoadoutFireResolutionOutcome(
            didFire: false,
            updatedLoadout: updatedLoadout,
            spawnedProjectile: null);
    }

    public static LoadoutFireResolutionOutcome Fired(
        TankWeaponLoadout updatedLoadout,
        ProjectileState spawnedProjectile)
    {
        ArgumentNullException.ThrowIfNull(updatedLoadout);

        return new LoadoutFireResolutionOutcome(
            didFire: true,
            updatedLoadout: updatedLoadout,
            spawnedProjectile: spawnedProjectile);
    }
}
