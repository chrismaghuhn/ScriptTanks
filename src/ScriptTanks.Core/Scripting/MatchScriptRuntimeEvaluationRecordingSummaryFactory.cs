using System;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure factory that builds a <see cref="MatchScriptRuntimeEvaluationRecordingSummary"/> from a
/// <see cref="MatchScriptRuntimeEvaluationRecording"/>.
/// </summary>
/// <remarks>
/// Derives metadata from recording order: <see cref="MatchScriptRuntimeEvaluationRecordingSummary.InitialTick"/>
/// is the first frame's tick, <see cref="MatchScriptRuntimeEvaluationRecordingSummary.FinalTick"/> is the last
/// frame's tick, and <see cref="MatchScriptRuntimeEvaluationRecordingSummary.TotalEvaluationRecords"/> is the sum
/// of each frame's <see cref="ScriptRuntimeEvaluationTrace.Count"/>. Does not validate frame-index continuity or
/// tick ordering, does not sort or normalize frames, does not record or play back frames, execute commands,
/// generate match, sensor, weapon, or movement requests, mutate match state, log, diagnose, or integrate Godot.
/// </remarks>
public static class MatchScriptRuntimeEvaluationRecordingSummaryFactory
{
    /// <summary>
    /// Creates a summary from the given recording's frames in list order.
    /// </summary>
    public static MatchScriptRuntimeEvaluationRecordingSummary Create(
        MatchScriptRuntimeEvaluationRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        int frameCount = recording.Frames.Count;
        SimTick initialTick = recording.Frames[0].Tick;
        SimTick finalTick = recording.Frames[recording.Frames.Count - 1].Tick;
        int totalEvaluationRecords = 0;
        for (int i = 0; i < recording.Frames.Count; i++)
        {
            totalEvaluationRecords += recording.Frames[i].Trace.Count;
        }

        return new MatchScriptRuntimeEvaluationRecordingSummary(
            frameCount,
            initialTick,
            finalTick,
            totalEvaluationRecords);
    }
}
