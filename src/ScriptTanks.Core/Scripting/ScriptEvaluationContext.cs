using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable snapshot of values for future script condition evaluation.
/// </summary>
/// <remarks>
/// Pure data model holding already-computed values. This type does not read
/// <c>MatchState</c>, evaluate conditions, select routines, execute commands, mutate match
/// state, call sensors, fire weapons, move tanks, log, record replays, or integrate Godot.
/// <para>
/// <see cref="TurretAimStatus"/> participates in equality by
/// <see cref="ScriptVisibleTurretAimStatusResult.Status"/> only — a condition snapshot by
/// status, not by result reference or target identity.
/// </para>
/// </remarks>
public readonly struct ScriptEvaluationContext : IEquatable<ScriptEvaluationContext>
{
    public bool EnemyVisible { get; }

    public bool WeaponReady { get; }

    public int MyHitPoints { get; }

    public Fixed EnemyDistance { get; }

    public bool SensorReady { get; }

    public ScriptVisibleTurretAimStatusResult TurretAimStatus { get; }

    public ScriptEvaluationContext(
        bool enemyVisible,
        bool weaponReady,
        int myHitPoints,
        Fixed enemyDistance,
        bool sensorReady,
        ScriptVisibleTurretAimStatusResult turretAimStatus)
    {
        ArgumentNullException.ThrowIfNull(turretAimStatus);

        if (myHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(myHitPoints),
                myHitPoints,
                "My hit points must not be negative.");
        }

        if (enemyDistance < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyDistance),
                enemyDistance,
                "Enemy distance must not be negative.");
        }

        EnemyVisible = enemyVisible;
        WeaponReady = weaponReady;
        MyHitPoints = myHitPoints;
        EnemyDistance = enemyDistance;
        SensorReady = sensorReady;
        TurretAimStatus = turretAimStatus;
    }

    public bool Equals(ScriptEvaluationContext other)
    {
        return EnemyVisible == other.EnemyVisible
            && WeaponReady == other.WeaponReady
            && MyHitPoints == other.MyHitPoints
            && EnemyDistance == other.EnemyDistance
            && SensorReady == other.SensorReady
            && TurretAimStatus.Status == other.TurretAimStatus.Status;
    }

    public override bool Equals(object? obj)
    {
        return obj is ScriptEvaluationContext other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            EnemyVisible,
            WeaponReady,
            MyHitPoints,
            EnemyDistance,
            SensorReady,
            TurretAimStatus.Status);
    }

    public static bool operator ==(ScriptEvaluationContext left, ScriptEvaluationContext right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScriptEvaluationContext left, ScriptEvaluationContext right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptEvaluationContext {{ EnemyVisible = {EnemyVisible}, WeaponReady = {WeaponReady}, MyHitPoints = {MyHitPoints}, EnemyDistance = {EnemyDistance}, SensorReady = {SensorReady}, TurretAimStatus = {TurretAimStatus.Status} }}");
    }
}
