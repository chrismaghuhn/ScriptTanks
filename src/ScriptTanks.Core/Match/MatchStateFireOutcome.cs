using System;
using ScriptTanks.Core.Projectiles;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable result of <see cref="MatchStateFireSystem.ResolveFire(MatchState, Ids.TankId, Weapons.WeaponSlot, Simulation.SimTick, Ids.ProjectileId, Math.FixedVec2, Math.FixedVec2)"/>.
/// Bundles whether a projectile was spawned, the resulting <see cref="MatchState"/>
/// snapshot (with the shooter's loadout replaced and, if fired, the spawned
/// projectile appended), and the spawned projectile itself when applicable.
/// </summary>
/// <remarks>
/// <para>
/// This type is a pure data carrier. It does not run gameplay systems,
/// advance ticks, step or hit-resolve projectiles, mutate match state, or
/// produce combat logs / replay frames / serialization / Godot output.
/// </para>
/// <para>
/// Construction is restricted to the named factory methods
/// <see cref="NotReady(MatchState)"/> and
/// <see cref="Fired(MatchState, ProjectileState)"/>. The private
/// constructor is the single source of truth for the
/// (<see cref="DidFire"/>, <see cref="SpawnedProjectile"/>) invariant and
/// throws <see cref="ArgumentException"/> for any inconsistent combination.
/// </para>
/// </remarks>
public sealed class MatchStateFireOutcome
{
    public bool DidFire { get; }

    public MatchState UpdatedState { get; }

    public ProjectileState? SpawnedProjectile { get; }

    public bool HasSpawnedProjectile => SpawnedProjectile.HasValue;

    private MatchStateFireOutcome(
        bool didFire,
        MatchState updatedState,
        ProjectileState? spawnedProjectile)
    {
        ArgumentNullException.ThrowIfNull(updatedState);

        if (didFire && !spawnedProjectile.HasValue)
        {
            throw new ArgumentException(
                "A fired outcome must include a spawned projectile.",
                nameof(spawnedProjectile));
        }

        if (!didFire && spawnedProjectile.HasValue)
        {
            throw new ArgumentException(
                "A not-ready outcome must not include a spawned projectile.",
                nameof(spawnedProjectile));
        }

        DidFire = didFire;
        UpdatedState = updatedState;
        SpawnedProjectile = spawnedProjectile;
    }

    public static MatchStateFireOutcome NotReady(MatchState updatedState)
        => new MatchStateFireOutcome(
            didFire: false,
            updatedState: updatedState,
            spawnedProjectile: null);

    public static MatchStateFireOutcome Fired(
        MatchState updatedState,
        ProjectileState spawnedProjectile)
        => new MatchStateFireOutcome(
            didFire: true,
            updatedState: updatedState,
            spawnedProjectile: spawnedProjectile);
}
