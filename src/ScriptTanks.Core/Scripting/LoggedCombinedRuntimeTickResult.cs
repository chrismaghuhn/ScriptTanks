using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Bundles one combined-runtime tick result with the combat-log snapshot
/// captured or composed for that tick.
/// </summary>
/// <remarks>
/// Pure data carrier for a future <c>LoggedCombinedRuntimeTickPipeline</c>.
/// Holds <see cref="CombinedRuntimeTickResult"/> and <see cref="CombatLog"/> only.
/// Does not execute ticks, invoke log factories, merge logs, create events, store
/// logs globally, integrate with runners, replay recorders, serialization, UI, or Godot.
/// Equality is reference-based.
/// </remarks>
public sealed class LoggedCombinedRuntimeTickResult
{
    public CombinedRuntimeTickResult TickResult { get; }

    public CombatLog Log { get; }

    public LoggedCombinedRuntimeTickResult(
        CombinedRuntimeTickResult tickResult,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(tickResult);
        ArgumentNullException.ThrowIfNull(log);

        TickResult = tickResult;
        Log = log;
    }
}
