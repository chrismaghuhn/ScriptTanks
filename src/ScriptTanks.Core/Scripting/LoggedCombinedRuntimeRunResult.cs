using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Bundles a completed combined-runtime run result with the combat-log snapshot
/// captured or composed for that run.
/// </summary>
/// <remarks>
/// Pure data carrier for a future <see cref="LoggedCombinedRuntimeRunner"/>.
/// Holds <see cref="CombinedRuntimeRunResult"/> and <see cref="CombatLog"/> only.
/// Does not execute matches, merge logs, create events, store logs globally,
/// integrate with replay recorders, pipelines, serialization, UI, or Godot.
/// Equality is reference-based.
/// </remarks>
public sealed class LoggedCombinedRuntimeRunResult
{
    public CombinedRuntimeRunResult RunResult { get; }

    public CombatLog Log { get; }

    public LoggedCombinedRuntimeRunResult(
        CombinedRuntimeRunResult runResult,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(log);

        RunResult = runResult;
        Log = log;
    }
}
