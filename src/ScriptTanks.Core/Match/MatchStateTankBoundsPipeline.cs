using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure deterministic arena outer-bounds correction for one tick snapshot.
/// Clamps each alive tank's circle (center + <see cref="BasicTankStats.HitboxRadius"/>)
/// inside <see cref="ArenaBounds"/> and returns per-tank outcomes.
/// </summary>
/// <remarks>
/// Assumes the arena is large enough that <c>min &lt;= max</c> per axis for each tank radius;
/// arenas smaller than <c>2 * HitboxRadius</c> are not handled here (setup/arena validation).
/// Does not advance <see cref="MatchState.CurrentTick"/>, step or resolve projectiles,
/// apply wall or obstacle collision, resolve tank-vs-tank collision, run
/// <see cref="MatchTickPipeline"/>, log, replay, or integrate Godot.
/// Mutates <see cref="MovementState.Position"/> only; preserves
/// <see cref="MovementState.VelocityPerTick"/>.
/// </remarks>
public static class MatchStateTankBoundsPipeline
{
    /// <summary>
    /// Applies arena outer-bounds clamping for every tank in <paramref name="state"/> in index order.
    /// </summary>
    /// <param name="state">Non-null match snapshot whose tank positions may be corrected.</param>
    /// <returns>
    /// Initial state reference, final state with updated tank positions where clamped, and one record per tank.
    /// </returns>
    public static MatchStateTankBoundsResult Step(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        TankState[] tanks = state.Tanks.ToArray();
        MatchStateTankBoundsRecord[] records =
            new MatchStateTankBoundsRecord[state.Tanks.Count];

        for (int i = 0; i < state.Tanks.Count; i++)
        {
            TankState tank = state.Tanks[i];
            FixedVec2 initialPosition = tank.Movement.Position;

            if (tank.IsDestroyed)
            {
                records[i] = MatchStateTankBoundsRecord.SkippedDestroyed(
                    i,
                    tank.Id,
                    initialPosition);
                continue;
            }

            FixedVec2 clampedPosition = ClampTankPositionToArena(
                initialPosition,
                tank.Definition.Stats.HitboxRadius,
                state.Arena.Bounds);

            if (clampedPosition.Equals(initialPosition))
            {
                records[i] = MatchStateTankBoundsRecord.InsideBounds(
                    i,
                    tank.Id,
                    initialPosition);
                continue;
            }

            MovementState clampedMovement = tank.Movement.WithPosition(clampedPosition);
            tanks[i] = tank.WithMovement(clampedMovement);

            records[i] = MatchStateTankBoundsRecord.Clamped(
                i,
                tank.Id,
                initialPosition,
                clampedPosition);
        }

        MatchState finalState = state.WithTanks(tanks);

        return new MatchStateTankBoundsResult(
            state,
            finalState,
            records);
    }

    private static FixedVec2 ClampTankPositionToArena(
        FixedVec2 position,
        Fixed radius,
        ArenaBounds bounds)
    {
        Fixed minX = radius;
        Fixed minY = radius;
        Fixed maxX = bounds.Width - radius;
        Fixed maxY = bounds.Height - radius;

        return new FixedVec2(
            Clamp(position.X, minX, maxX),
            Clamp(position.Y, minY, maxY));
    }

    private static Fixed Clamp(Fixed value, Fixed min, Fixed max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
