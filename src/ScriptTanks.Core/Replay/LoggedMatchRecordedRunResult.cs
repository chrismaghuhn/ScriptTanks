using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Bundles a recorded match run result with the combat-log snapshot captured
/// or composed for that recorded run.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not execute matches, record replays,
/// emit logs, store logs globally, integrate with runners, pipelines,
/// serialization, UI, or Godot. Equality is reference-based.
/// </remarks>
public sealed class LoggedMatchRecordedRunResult
{
    public MatchRecordedRunResult RecordedRunResult { get; }

    public CombatLog Log { get; }

    public LoggedMatchRecordedRunResult(
        MatchRecordedRunResult recordedRunResult,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(recordedRunResult);
        ArgumentNullException.ThrowIfNull(log);

        RecordedRunResult = recordedRunResult;
        Log = log;
    }
}
