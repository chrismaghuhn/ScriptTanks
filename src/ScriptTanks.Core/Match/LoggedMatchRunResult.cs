using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Bundles a completed match run result with the combat-log snapshot captured
/// or composed for that run.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not execute matches, emit logs, store logs
/// globally, integrate with runners, replay recorders, pipelines,
/// serialization, UI, or Godot. Equality is reference-based.
/// </remarks>
public sealed class LoggedMatchRunResult
{
    public MatchRunResult RunResult { get; }

    public CombatLog Log { get; }

    public LoggedMatchRunResult(
        MatchRunResult runResult,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(log);

        RunResult = runResult;
        Log = log;
    }
}
