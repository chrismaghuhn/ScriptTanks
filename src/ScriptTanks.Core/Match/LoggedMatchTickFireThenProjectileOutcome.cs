using System;
using ScriptTanks.Core.Logging;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Bundles the result of one fire-then-projectile tick with the combat-log
/// snapshot describing the optional fire request.
/// </summary>
/// <remarks>
/// Pure data carrier. This type does not emit logs, store logs globally,
/// integrate with runners, replay recorders, serialization, UI, or Godot.
/// The log currently covers fire request/result events only; projectile-hit,
/// damage, cleanup, and match-end logs are added by future tasks.
/// Equality is reference-based.
/// </remarks>
public sealed class LoggedMatchTickFireThenProjectileOutcome
{
    public MatchTickFireThenProjectileOutcome TickOutcome { get; }

    public CombatLog Log { get; }

    public LoggedMatchTickFireThenProjectileOutcome(
        MatchTickFireThenProjectileOutcome tickOutcome,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(tickOutcome);
        ArgumentNullException.ThrowIfNull(log);

        TickOutcome = tickOutcome;
        Log = log;
    }
}
