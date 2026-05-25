using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Bundles the result of resolving one fire request with the combat-log
/// snapshot describing that request.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not emit logs, store logs globally,
/// integrate with runners, replay recorders, pipelines, serialization,
/// UI, or Godot. Equality is reference-based.
/// </remarks>
public sealed class LoggedMatchStateFireOutcome
{
    public MatchStateFireOutcome FireOutcome { get; }

    public CombatLog Log { get; }

    public LoggedMatchStateFireOutcome(
        MatchStateFireOutcome fireOutcome,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(fireOutcome);
        ArgumentNullException.ThrowIfNull(log);

        FireOutcome = fireOutcome;
        Log = log;
    }
}
