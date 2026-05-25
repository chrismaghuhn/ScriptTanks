using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable container for a future match-level script runtime evaluation recording run outcome.
/// </summary>
/// <remarks>
/// Bundles a <see cref="MatchScriptRuntimeEvaluationRecording"/> with a
/// <see cref="MatchScriptRuntimeEvaluationRecordingSummary"/>. Does not validate that the summary matches the
/// recording, does not record or play back frames by itself, execute commands, generate match, sensor, weapon,
/// or movement requests, mutate match state, log, diagnose, or integrate Godot.
/// </remarks>
public sealed class MatchScriptRuntimeEvaluationRecordedRunResult
{
    public MatchScriptRuntimeEvaluationRecording Recording { get; }

    public MatchScriptRuntimeEvaluationRecordingSummary Summary { get; }

    public MatchScriptRuntimeEvaluationRecordedRunResult(
        MatchScriptRuntimeEvaluationRecording recording,
        MatchScriptRuntimeEvaluationRecordingSummary summary)
    {
        ArgumentNullException.ThrowIfNull(recording);
        ArgumentNullException.ThrowIfNull(summary);

        Recording = recording;
        Summary = summary;
    }
}
