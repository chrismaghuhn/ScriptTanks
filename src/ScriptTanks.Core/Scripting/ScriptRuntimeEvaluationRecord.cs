using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable association of a script program, evaluation context, and decision pipeline output with a
/// simulation tick and tank index for future runtime reporting.
/// </summary>
/// <remarks>
/// Pure data carrier: does not validate <see cref="TankIndex"/> against match state or verify that
/// <see cref="Result"/> was derived from <see cref="Program"/> and <see cref="Context"/>. This type
/// does not evaluate scripts, log, diagnose, format output, execute commands, generate match,
/// sensor, weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeEvaluationRecord
{
    public SimTick Tick { get; }

    public int TankIndex { get; }

    public ScriptProgram Program { get; }

    public ScriptEvaluationContext Context { get; }

    public ScriptRuntimeDecisionResult Result { get; }

    public ScriptRuntimeEvaluationRecord(
        SimTick tick,
        int tankIndex,
        ScriptProgram program,
        ScriptEvaluationContext context,
        ScriptRuntimeDecisionResult result)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(result);

        Tick = tick;
        TankIndex = tankIndex;
        Program = program;
        Context = context;
        Result = result;
    }
}
