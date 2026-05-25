namespace ScriptTanks.Core.Match;

/// <summary>
/// Categorical match-setup validation issues reported by
/// <see cref="MatchSetupValidator"/>. Values are intentionally simple
/// enum tokens without messages, payloads, tank indices, or severity
/// levels in this task. Higher-level UI/coach/debug layers map these
/// values to user-facing text in later tasks.
/// </summary>
public enum MatchSetupValidationIssue
{
    DuplicateTankId,
    DuplicateOwnerSlot,
    TankDestroyedAtStart,
    TankPositionOutsideArenaBounds,
    TankHitboxOutsideArenaBounds,
    TankHitboxIntersectsWall,
}
