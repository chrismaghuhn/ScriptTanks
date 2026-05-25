using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable input for a future script runtime evaluation: tank index, program, and precomputed
/// evaluation context.
/// </summary>
/// <remarks>
/// Pure data carrier for per-tank evaluation requests; a shared
/// <see cref="ScriptTanks.Core.Simulation.SimTick"/> can be supplied separately by a future batch layer.
/// This type does not validate <see cref="TankIndex"/> against match state, evaluate scripts, execute commands, generate match, sensor, weapon, or
/// movement requests, log, diagnose, mutate match state, or integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeEvaluationRequest
{
    public int TankIndex { get; }

    public ScriptProgram Program { get; }

    public ScriptEvaluationContext Context { get; }

    public ScriptRuntimeEvaluationRequest(
        int tankIndex,
        ScriptProgram program,
        ScriptEvaluationContext context)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(program);

        TankIndex = tankIndex;
        Program = program;
        Context = context;
    }
}
