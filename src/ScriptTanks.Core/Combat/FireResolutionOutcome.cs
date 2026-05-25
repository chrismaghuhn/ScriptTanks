using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Immutable result of resolving a single weapon fire attempt. Holds the
/// updated weapon cooldown state and, if a shot was fired, the spawned
/// projectile. The class does not mutate loadouts, projectile collections,
/// match state, combat logs, replay frames, or any external container.
/// <para>
/// Construction is restricted to the named factory methods
/// <see cref="NotReady(WeaponState)"/> and
/// <see cref="Fired(WeaponState, ProjectileState)"/> to keep the
/// (DidFire, SpawnedProjectile) invariant consistent.
/// </para>
/// </summary>
public sealed class FireResolutionOutcome
{
    public bool DidFire { get; }

    public WeaponState UpdatedWeapon { get; }

    public ProjectileState? SpawnedProjectile { get; }

    private FireResolutionOutcome(
        bool didFire,
        WeaponState updatedWeapon,
        ProjectileState? spawnedProjectile)
    {
        DidFire = didFire;
        UpdatedWeapon = updatedWeapon;
        SpawnedProjectile = spawnedProjectile;
    }

    public static FireResolutionOutcome NotReady(WeaponState weapon)
        => new FireResolutionOutcome(
            didFire: false,
            updatedWeapon: weapon,
            spawnedProjectile: null);

    public static FireResolutionOutcome Fired(
        WeaponState updatedWeapon,
        ProjectileState spawnedProjectile)
        => new FireResolutionOutcome(
            didFire: true,
            updatedWeapon: updatedWeapon,
            spawnedProjectile: spawnedProjectile);
}
