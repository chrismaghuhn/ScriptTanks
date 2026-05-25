using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable association of a <see cref="ScriptRuntimeDecisionResult"/> with a simulation tick and
/// tank index for future script runtime debugging.
/// </summary>
/// <remarks>
/// Pure data carrier: bundles decision pipeline output with temporal and player/tank context. This
/// type does not validate <see cref="TankIndex"/> against match state, log, diagnose, format output,
/// execute commands, generate match, sensor, weapon, or movement requests, mutate match state, or
/// integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeDecisionSnapshot
{
    public SimTick Tick { get; }

    public int TankIndex { get; }

    public ScriptRuntimeDecisionResult Result { get; }

    public ScriptRuntimeDecisionSnapshot(
        SimTick tick,
        int tankIndex,
        ScriptRuntimeDecisionResult result)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(result);

        Tick = tick;
        TankIndex = tankIndex;
        Result = result;
    }
}
