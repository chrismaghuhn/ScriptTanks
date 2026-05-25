using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable initial match snapshot containing the selected arena, the start
/// tick, and the initial tank states. This type performs only structural
/// validation (non-null arena, non-null and non-empty tank collection) and
/// does not run or validate gameplay rules. Duplicate <see cref="TankState.Id"/>
/// values, duplicate <see cref="TankState.OwnerSlot"/> values, destroyed
/// tanks, out-of-bounds positions, and spawn-in-wall placements are
/// intentionally not rejected here; that responsibility belongs to a later
/// match-setup validator. Tank order is preserved exactly as provided.
/// </summary>
public sealed class MatchInitialState
{
    public ArenaDefinition Arena { get; }

    public SimTick StartTick { get; }

    public IReadOnlyList<TankState> Tanks { get; }

    public MatchInitialState(
        ArenaDefinition arena,
        IEnumerable<TankState> tanks)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(tanks);

        TankState[] tankArray = tanks.ToArray();

        if (tankArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one tank is required.",
                nameof(tanks));
        }

        Arena = arena;
        StartTick = SimTick.Zero;
        Tanks = tankArray;
    }
}
