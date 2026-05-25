using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Bundles the run result of a recorded match with the full-state replay
/// recording captured during that run.
/// </summary>
/// <remarks>
/// Pure data carrier. No equality override; reference identity applies.
/// Out of scope: serialization, playback, logs, runner integration, Godot.
/// </remarks>
public sealed class MatchRecordedRunResult
{
    public MatchRunResult RunResult { get; }

    public MatchReplayRecording Recording { get; }

    public MatchRecordedRunResult(
        MatchRunResult runResult,
        MatchReplayRecording recording)
    {
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(recording);

        RunResult = runResult;
        Recording = recording;
    }
}
