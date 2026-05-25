using System;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Movement;

/// <summary>
/// Pure deterministic per-tick movement integrator. Tick-native: never uses
/// <see cref="ScriptTanks.Core.Simulation.SimConstants.FixedDeltaTime"/>.
/// </summary>
public static class MovementIntegrator
{
    public static MovementState Step(MovementState state)
    {
        return state.WithPosition(state.Position + state.VelocityPerTick);
    }

    public static MovementState Step(MovementState state, int tickCount)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "tickCount must be non-negative.");
        }

        if (tickCount == 0)
        {
            return state;
        }

        return state.WithPosition(
            state.Position + state.VelocityPerTick * Fixed.FromInt(tickCount));
    }
}
