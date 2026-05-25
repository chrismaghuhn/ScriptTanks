using System;
using System.Linq;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure deterministic tank movement integration for one tick snapshot.
/// Applies <see cref="MovementIntegrator"/> to each alive tank's
/// <see cref="MovementState.VelocityPerTick"/> and returns per-tank outcomes.
/// </summary>
/// <remarks>
/// Does not advance <see cref="MatchState.CurrentTick"/>, step or resolve projectiles,
/// apply arena bounds or collision, run <see cref="MatchTickPipeline"/>,
/// log, replay, or integrate Godot.
/// </remarks>
public static class MatchStateTankMovementPipeline
{
    /// <summary>
    /// Integrates tank movement for every tank in <paramref name="state"/> in index order.
    /// </summary>
    /// <param name="state">Non-null match snapshot whose tanks are integrated.</param>
    /// <returns>
    /// Initial state reference, final state with updated tank movements, and one record per tank.
    /// </returns>
    public static MatchStateTankMovementResult Step(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        TankState[] tanks = state.Tanks.ToArray();
        MatchStateTankMovementRecord[] records =
            new MatchStateTankMovementRecord[state.Tanks.Count];

        for (int i = 0; i < state.Tanks.Count; i++)
        {
            TankState tank = state.Tanks[i];

            if (tank.IsDestroyed)
            {
                records[i] = MatchStateTankMovementRecord.SkippedDestroyed(
                    i,
                    tank.Id,
                    tank.Movement);
                continue;
            }

            if (tank.Movement.VelocityPerTick == FixedVec2.Zero)
            {
                records[i] = MatchStateTankMovementRecord.StayedStill(
                    i,
                    tank.Id,
                    tank.Movement);
                continue;
            }

            MovementState movedMovement = MovementIntegrator.Step(tank.Movement);

            tanks[i] = tank.WithMovement(movedMovement);

            records[i] = MatchStateTankMovementRecord.Moved(
                i,
                tank.Id,
                tank.Movement,
                movedMovement);
        }

        MatchState finalState = state.WithTanks(tanks);

        return new MatchStateTankMovementResult(
            state,
            finalState,
            records);
    }
}
