using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable aggregate metadata for a match-level script runtime evaluation recording.
/// </summary>
/// <remarks>
/// Pure data carrier for values a future summary factory may derive from a
/// <see cref="MatchScriptRuntimeEvaluationRecording"/>. Requires at least one frame count and
/// <see cref="FinalTick"/> not before <see cref="InitialTick"/>. <see cref="TotalEvaluationRecords"/> may be
/// zero. This type does not validate against a concrete recording, record or play back frames, execute
/// commands, generate match, sensor, weapon, or movement requests, mutate match state, log, diagnose,
/// or integrate Godot.
/// </remarks>
public sealed class MatchScriptRuntimeEvaluationRecordingSummary
{
    public int FrameCount { get; }

    public SimTick InitialTick { get; }

    public SimTick FinalTick { get; }

    public int TotalEvaluationRecords { get; }

    public MatchScriptRuntimeEvaluationRecordingSummary(
        int frameCount,
        SimTick initialTick,
        SimTick finalTick,
        int totalEvaluationRecords)
    {
        if (frameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameCount),
                frameCount,
                "Frame count must be positive.");
        }

        if (finalTick < initialTick)
        {
            throw new ArgumentException(
                "Final tick must not be before the initial tick.",
                nameof(finalTick));
        }

        if (totalEvaluationRecords < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalEvaluationRecords),
                totalEvaluationRecords,
                "Total evaluation records must not be negative.");
        }

        FrameCount = frameCount;
        InitialTick = initialTick;
        FinalTick = finalTick;
        TotalEvaluationRecords = totalEvaluationRecords;
    }
}
