using System;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Replay;

namespace ScriptTanks.Core.Diagnostics;

/// <summary>
/// Immutable debug snapshot that bundles a match-state snapshot with optional
/// combat-log and replay-frame context.
/// </summary>
/// <remarks>
/// Pure data carrier for future debug overlays, replay inspection, and tooling.
/// This type does not execute matches, record replays, emit logs, format logs,
/// serialize data, render UI, integrate with Godot, or mutate the wrapped
/// state/log/frame objects. Equality is reference-based.
/// </remarks>
public sealed class MatchDebugSnapshot
{
    public MatchState State { get; }

    public CombatLog? Log { get; }

    public MatchReplayFrame? ReplayFrame { get; }

    public bool HasReplayFrame => ReplayFrame is not null;

    public MatchDebugSnapshot(
        MatchState state,
        CombatLog? log = null,
        MatchReplayFrame? replayFrame = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        State = state;
        Log = log;
        ReplayFrame = replayFrame;
    }

    public static MatchDebugSnapshot FromState(MatchState state)
    {
        return new MatchDebugSnapshot(state);
    }

    public static MatchDebugSnapshot FromStateAndLog(
        MatchState state,
        CombatLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new MatchDebugSnapshot(state, log);
    }

    public static MatchDebugSnapshot FromReplayFrame(
        MatchReplayFrame replayFrame,
        CombatLog? log = null)
    {
        ArgumentNullException.ThrowIfNull(replayFrame);
        return new MatchDebugSnapshot(replayFrame.State, log, replayFrame);
    }
}
