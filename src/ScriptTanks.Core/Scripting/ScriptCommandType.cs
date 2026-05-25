namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Identifies a player-authored script command for future tank logic.
/// </summary>
public enum ScriptCommandType
{
    NoOp = 0,
    ScanEnemy = 1,
    AimAtEnemy = 2,
    Fire = 3,
    MoveToPatrolPoint = 4,
    Retreat = 5,
}
