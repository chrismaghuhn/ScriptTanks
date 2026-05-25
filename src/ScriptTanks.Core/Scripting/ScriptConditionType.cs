namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Identifies a player-authored script condition for future tank logic.
/// </summary>
public enum ScriptConditionType
{
    Always = 0,
    EnemyVisible = 1,
    WeaponReady = 2,
    MyHpBelow = 3,
    EnemyDistanceBelow = 4,
    SensorReady = 5,
    TurretAligned = 6,
    TurretTurning = 7,
    HasAimTarget = 8,
    MissingAimSolution = 9,
}
