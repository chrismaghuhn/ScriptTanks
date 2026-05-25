using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable association of a frame index, simulation tick, and script evaluation trace for future
/// match-level script evaluation recording.
/// </summary>
/// <remarks>
/// Pure data carrier: does not validate that <see cref="Trace"/> contents align with
/// <see cref="Tick"/>, record frames, play back replays, evaluate scripts, execute commands, generate
/// match, sensor, weapon, or movement requests, mutate match state, log, diagnose, or integrate Godot.
/// </remarks>
public sealed class MatchScriptRuntimeEvaluationFrame
{
    public int FrameIndex { get; }

    public SimTick Tick { get; }

    public ScriptRuntimeEvaluationTrace Trace { get; }

    public MatchScriptRuntimeEvaluationFrame(
        int frameIndex,
        SimTick tick,
        ScriptRuntimeEvaluationTrace trace)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                "Frame index must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(trace);

        FrameIndex = frameIndex;
        Tick = tick;
        Trace = trace;
    }
}
