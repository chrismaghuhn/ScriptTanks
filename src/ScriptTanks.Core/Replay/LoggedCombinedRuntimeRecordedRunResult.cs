using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Bundles a recorded combined-runtime match run result with the combat-log
/// snapshot captured or composed for that recorded run.
/// </summary>
/// <remarks>
/// Pure data carrier for a future <see cref="LoggedCombinedRuntimeReplayRecorder"/>.
/// Holds <see cref="MatchRecordedRunResult"/> and <see cref="CombatLog"/> only.
/// Does not execute matches, record replays, merge logs, create events, store
/// logs globally, integrate with runners, pipelines, serialization, UI, or Godot.
/// Equality is reference-based.
/// </remarks>
public sealed class LoggedCombinedRuntimeRecordedRunResult
{
    public MatchRecordedRunResult RecordedRunResult { get; }

    public CombatLog Log { get; }

    public LoggedCombinedRuntimeRecordedRunResult(
        MatchRecordedRunResult recordedRunResult,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(recordedRunResult);
        ArgumentNullException.ThrowIfNull(log);

        RecordedRunResult = recordedRunResult;
        Log = log;
    }
}
