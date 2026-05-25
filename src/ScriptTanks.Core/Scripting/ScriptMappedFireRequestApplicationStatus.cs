namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome classification for a future script-mapped fire request application attempt.
/// </summary>
/// <remarks>
/// Pure status labels only; does not call <see cref="Match.MatchStateFireSystem"/>,
/// mutate match state, log, replay, or integrate Godot.
/// </remarks>
public enum ScriptMappedFireRequestApplicationStatus
{
    SkippedNotConstructed = 0,

    Applied = 1,

    FireRejected = 2,
}
