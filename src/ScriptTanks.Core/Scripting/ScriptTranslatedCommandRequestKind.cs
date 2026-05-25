namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Classification of a future translated script command request produced by command translation.
/// </summary>
/// <remarks>
/// Pure enum labels for <see cref="ScriptTranslatedCommandRequest"/>; values do not execute sensors,
/// weapons, movement, or any gameplay kernel behavior.
/// </remarks>
public enum ScriptTranslatedCommandRequestKind
{
    None = 0,

    NoOp = 1,

    ScanEnemy = 2,

    AimAtEnemy = 3,

    Fire = 4,

    MoveToPatrolPoint = 5,

    Retreat = 6,
}
