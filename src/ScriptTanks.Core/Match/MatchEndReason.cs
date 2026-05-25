namespace ScriptTanks.Core.Match;

/// <summary>
/// Why a match has ended, as determined by
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/>.
/// </summary>
/// <remarks>
/// Values are explicit so they can later be referenced by replay frames or
/// log events without depending on enum-member ordering. This enum is a
/// pure data classification — it does not encode any gameplay rule, team
/// logic, ranking logic, or runner behavior.
/// </remarks>
public enum MatchEndReason
{
    /// <summary>The match is still running. No end condition has fired.</summary>
    None = 0,

    /// <summary>Exactly one tank is alive and at least one other tank is destroyed.</summary>
    TankDestroyed = 1,

    /// <summary>The tick budget was exhausted and exactly one tank holds the highest <c>CurrentHitPoints</c>.</summary>
    TimeoutHpAdvantage = 2,

    /// <summary>The tick budget was exhausted and the highest <c>CurrentHitPoints</c> is shared by multiple tanks.</summary>
    TimeoutDraw = 3,
}
