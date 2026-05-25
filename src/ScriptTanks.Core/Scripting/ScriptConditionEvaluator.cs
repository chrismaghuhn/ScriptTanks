using System;
using System.Globalization;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure evaluator that resolves a <see cref="ScriptCondition"/> against a
/// <see cref="ScriptEvaluationContext"/>.
/// </summary>
/// <remarks>
/// Supports the fixed MVP condition types plus turret aim status conditions
/// (<see cref="ScriptConditionType.TurretAligned"/>,
/// <see cref="ScriptConditionType.TurretTurning"/>,
/// <see cref="ScriptConditionType.HasAimTarget"/>,
/// <see cref="ScriptConditionType.MissingAimSolution"/>). Numeric thresholds are parsed from
/// <see cref="ScriptCondition.Argument"/> for <see cref="ScriptConditionType.MyHpBelow"/> and
/// <see cref="ScriptConditionType.EnemyDistanceBelow"/> only. This type does not parse script
/// source text, select routines, execute commands, build context from match state, call sensors,
/// fire weapons, move tanks, log, record replays, or integrate Godot.
/// </remarks>
public static class ScriptConditionEvaluator
{
    public static bool Evaluate(
        ScriptCondition condition,
        ScriptEvaluationContext context)
    {
        return condition.Type switch
        {
            ScriptConditionType.Always => true,
            ScriptConditionType.EnemyVisible => context.EnemyVisible,
            ScriptConditionType.WeaponReady => context.WeaponReady,
            ScriptConditionType.SensorReady => context.SensorReady,
            ScriptConditionType.MyHpBelow =>
                context.MyHitPoints < ParseNonNegativeInt(condition),
            ScriptConditionType.EnemyDistanceBelow =>
                context.EnemyDistance < Fixed.FromInt(ParseNonNegativeInt(condition)),
            ScriptConditionType.TurretAligned =>
                context.TurretAimStatus.Status == ScriptVisibleTurretAimStatus.Aligned,
            ScriptConditionType.TurretTurning =>
                context.TurretAimStatus.Status == ScriptVisibleTurretAimStatus.Turning,
            ScriptConditionType.HasAimTarget =>
                context.TurretAimStatus.HasTarget,
            ScriptConditionType.MissingAimSolution =>
                context.TurretAimStatus.Status == ScriptVisibleTurretAimStatus.MissingAimSolution,
            _ => throw new ArgumentOutOfRangeException(
                nameof(condition),
                condition,
                "Unsupported script condition type."),
        };
    }

    private static int ParseNonNegativeInt(ScriptCondition condition)
    {
        if (!int.TryParse(
            condition.Argument,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int value))
        {
            throw new ArgumentException(
                "Script condition argument must be a non-negative integer.",
                nameof(condition));
        }

        if (value < 0)
        {
            throw new ArgumentException(
                "Script condition argument must be a non-negative integer.",
                nameof(condition));
        }

        return value;
    }
}
