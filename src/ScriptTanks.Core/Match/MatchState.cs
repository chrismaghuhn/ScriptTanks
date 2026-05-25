using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable runtime match snapshot. Bundles the selected
/// <see cref="ArenaDefinition"/>, the current simulation tick, the runtime
/// tank states, the per-tank weapon loadouts (paired with tanks by index),
/// and the active/inactive projectile states.
/// <para>
/// This type is a pure container. It does not advance ticks, run movement,
/// resolve fire attempts, move projectiles, detect hits, apply damage,
/// create combat logs, create replay frames, determine winners, or perform
/// any kind of serialization. Tank, loadout, and projectile order is
/// preserved exactly as supplied; input collections are defensively copied
/// so later mutations of the source do not leak into the snapshot.
/// </para>
/// <para>
/// Constructor validation is intentionally limited to structural
/// requirements: non-null arena/collections, at least one tank, and
/// <c>loadouts.Count == tanks.Count</c> (loadouts are paired with tanks by
/// index). Higher-level gameplay validation - e.g. duplicate
/// <see cref="TankState.Id"/> values, duplicate
/// <see cref="TankState.OwnerSlot"/> values, spawn-in-wall placements, or
/// destroyed initial tanks - is intentionally not performed here and belongs
/// to a dedicated match-setup validator.
/// </para>
/// </summary>
public sealed class MatchState
{
    public ArenaDefinition Arena { get; }

    public SimTick CurrentTick { get; }

    public IReadOnlyList<TankState> Tanks { get; }

    public IReadOnlyList<TankWeaponLoadout> Loadouts { get; }

    public IReadOnlyList<ProjectileState> Projectiles { get; }

    public MatchState(
        ArenaDefinition arena,
        SimTick currentTick,
        IEnumerable<TankState> tanks,
        IEnumerable<TankWeaponLoadout> loadouts,
        IEnumerable<ProjectileState> projectiles)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(tanks);
        ArgumentNullException.ThrowIfNull(loadouts);
        ArgumentNullException.ThrowIfNull(projectiles);

        TankState[] tankArray = tanks.ToArray();
        TankWeaponLoadout[] loadoutArray = loadouts.ToArray();
        ProjectileState[] projectileArray = projectiles.ToArray();

        if (tankArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one tank is required.",
                nameof(tanks));
        }

        if (loadoutArray.Length != tankArray.Length)
        {
            throw new ArgumentException(
                "Loadout count must match tank count.",
                nameof(loadouts));
        }

        Arena = arena;
        CurrentTick = currentTick;
        Tanks = tankArray;
        Loadouts = loadoutArray;
        Projectiles = projectileArray;
    }

    public MatchState WithCurrentTick(SimTick currentTick)
        => new MatchState(Arena, currentTick, Tanks, Loadouts, Projectiles);

    public MatchState WithTanks(IEnumerable<TankState> tanks)
        => new MatchState(Arena, CurrentTick, tanks, Loadouts, Projectiles);

    public MatchState WithLoadouts(IEnumerable<TankWeaponLoadout> loadouts)
        => new MatchState(Arena, CurrentTick, Tanks, loadouts, Projectiles);

    public MatchState WithProjectiles(IEnumerable<ProjectileState> projectiles)
        => new MatchState(Arena, CurrentTick, Tanks, Loadouts, projectiles);
}
