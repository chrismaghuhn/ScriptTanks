using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure result model for script-visible turret aim and alignment status.
/// </summary>
/// <remarks>
/// Immutable data carrier for computed aim/alignment status. Does not select targets, resolve
/// aim direction, evaluate script conditions, gate fire, mutate match state, log, replay, or
/// integrate Godot.
/// </remarks>
public sealed class ScriptVisibleTurretAimStatusResult
{
    public ScriptVisibleTurretAimStatus Status { get; }

    public TankId TankId { get; }

    public int TankIndex { get; }

    public TankId? TargetTankId { get; }

    public int? TargetTankIndex { get; }

    public Fixed CurrentRotation { get; }

    public Fixed? DesiredRotation { get; }

    public Fixed? AlignmentDelta { get; }

    public Fixed? AlignmentErrorMagnitude { get; }

    public bool IsAligned { get; }

    public bool HasTarget => TargetTankId.HasValue;

    public ScriptVisibleTurretAimStatusResult(
        ScriptVisibleTurretAimStatus status,
        TankId tankId,
        int tankIndex,
        TankId? targetTankId,
        int? targetTankIndex,
        Fixed currentRotation,
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        if (targetTankIndex is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetTankIndex),
                targetTankIndex,
                "Target tank index must not be negative.");
        }

        if (alignmentErrorMagnitude is { } magnitude && magnitude < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alignmentErrorMagnitude),
                alignmentErrorMagnitude,
                "Alignment error magnitude must not be negative.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Turret aim status must be a defined enum value.");
        }

        ValidateInvariants(
            status,
            targetTankId,
            targetTankIndex,
            desiredRotation,
            alignmentDelta,
            alignmentErrorMagnitude,
            isAligned);

        Status = status;
        TankId = tankId;
        TankIndex = tankIndex;
        TargetTankId = targetTankId;
        TargetTankIndex = targetTankIndex;
        CurrentRotation = currentRotation;
        DesiredRotation = desiredRotation;
        AlignmentDelta = alignmentDelta;
        AlignmentErrorMagnitude = alignmentErrorMagnitude;
        IsAligned = isAligned;
    }

    public static ScriptVisibleTurretAimStatusResult NoTarget(
        TankId tankId,
        int tankIndex,
        Fixed currentRotation)
    {
        return new ScriptVisibleTurretAimStatusResult(
            ScriptVisibleTurretAimStatus.NoTarget,
            tankId,
            tankIndex,
            targetTankId: null,
            targetTankIndex: null,
            currentRotation,
            desiredRotation: null,
            alignmentDelta: null,
            alignmentErrorMagnitude: null,
            isAligned: false);
    }

    public static ScriptVisibleTurretAimStatusResult OwnerDestroyed(
        TankId tankId,
        int tankIndex,
        Fixed currentRotation)
    {
        return new ScriptVisibleTurretAimStatusResult(
            ScriptVisibleTurretAimStatus.OwnerDestroyed,
            tankId,
            tankIndex,
            targetTankId: null,
            targetTankIndex: null,
            currentRotation,
            desiredRotation: null,
            alignmentDelta: null,
            alignmentErrorMagnitude: null,
            isAligned: false);
    }

    public static ScriptVisibleTurretAimStatusResult MissingAimSolution(
        TankId tankId,
        int tankIndex,
        TankId targetTankId,
        int targetTankIndex,
        Fixed currentRotation)
    {
        return new ScriptVisibleTurretAimStatusResult(
            ScriptVisibleTurretAimStatus.MissingAimSolution,
            tankId,
            tankIndex,
            targetTankId,
            targetTankIndex,
            currentRotation,
            desiredRotation: null,
            alignmentDelta: null,
            alignmentErrorMagnitude: null,
            isAligned: false);
    }

    public static ScriptVisibleTurretAimStatusResult Aligned(
        TankId tankId,
        int tankIndex,
        TankId targetTankId,
        int targetTankIndex,
        Fixed currentRotation,
        Fixed desiredRotation)
    {
        return new ScriptVisibleTurretAimStatusResult(
            ScriptVisibleTurretAimStatus.Aligned,
            tankId,
            tankIndex,
            targetTankId,
            targetTankIndex,
            currentRotation,
            desiredRotation,
            alignmentDelta: Fixed.Zero,
            alignmentErrorMagnitude: Fixed.Zero,
            isAligned: true);
    }

    public static ScriptVisibleTurretAimStatusResult Turning(
        TankId tankId,
        int tankIndex,
        TankId targetTankId,
        int targetTankIndex,
        Fixed currentRotation,
        Fixed desiredRotation,
        Fixed alignmentDelta,
        Fixed alignmentErrorMagnitude)
    {
        return new ScriptVisibleTurretAimStatusResult(
            ScriptVisibleTurretAimStatus.Turning,
            tankId,
            tankIndex,
            targetTankId,
            targetTankIndex,
            currentRotation,
            desiredRotation,
            alignmentDelta,
            alignmentErrorMagnitude,
            isAligned: false);
    }

    private static void ValidateInvariants(
        ScriptVisibleTurretAimStatus status,
        TankId? targetTankId,
        int? targetTankIndex,
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        switch (status)
        {
            case ScriptVisibleTurretAimStatus.NoTarget:
            case ScriptVisibleTurretAimStatus.OwnerDestroyed:
                RequireNoTargetFields(targetTankId, targetTankIndex, desiredRotation, alignmentDelta, alignmentErrorMagnitude, isAligned);
                break;

            case ScriptVisibleTurretAimStatus.MissingAimSolution:
                RequireTargetFields(targetTankId, targetTankIndex);
                RequireNoDesiredOrAlignmentFields(desiredRotation, alignmentDelta, alignmentErrorMagnitude, isAligned);
                break;

            case ScriptVisibleTurretAimStatus.Aligned:
                RequireTargetFields(targetTankId, targetTankIndex);
                RequireAlignedFields(desiredRotation, alignmentDelta, alignmentErrorMagnitude, isAligned);
                break;

            case ScriptVisibleTurretAimStatus.Turning:
                RequireTargetFields(targetTankId, targetTankIndex);
                RequireTurningFields(desiredRotation, alignmentDelta, alignmentErrorMagnitude, isAligned);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Turret aim status must be a defined enum value.");
        }
    }

    private static void RequireNoTargetFields(
        TankId? targetTankId,
        int? targetTankIndex,
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        if (targetTankId is not null
            || targetTankIndex is not null
            || desiredRotation is not null
            || alignmentDelta is not null
            || alignmentErrorMagnitude is not null
            || isAligned)
        {
            throw new ArgumentException(
                "No-target and owner-destroyed outcomes must not carry target, desired, or alignment fields.",
                "status");
        }
    }

    private static void RequireTargetFields(TankId? targetTankId, int? targetTankIndex)
    {
        if (targetTankId is null || targetTankIndex is null)
        {
            throw new ArgumentException(
                "Target-bearing aim status requires target tank identity and index.",
                "status");
        }
    }

    private static void RequireNoDesiredOrAlignmentFields(
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        if (desiredRotation is not null
            || alignmentDelta is not null
            || alignmentErrorMagnitude is not null
            || isAligned)
        {
            throw new ArgumentException(
                "Missing aim solution must not carry desired rotation or alignment fields.",
                "status");
        }
    }

    private static void RequireAlignedFields(
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        if (desiredRotation is null
            || alignmentDelta is null
            || alignmentErrorMagnitude is null
            || !isAligned
            || alignmentDelta != Fixed.Zero
            || alignmentErrorMagnitude != Fixed.Zero)
        {
            throw new ArgumentException(
                "Aligned status requires desired rotation, zero alignment delta, zero error magnitude, and IsAligned true.",
                "status");
        }
    }

    private static void RequireTurningFields(
        Fixed? desiredRotation,
        Fixed? alignmentDelta,
        Fixed? alignmentErrorMagnitude,
        bool isAligned)
    {
        if (desiredRotation is null
            || alignmentDelta is null
            || alignmentErrorMagnitude is null
            || isAligned
            || alignmentErrorMagnitude <= Fixed.Zero)
        {
            throw new ArgumentException(
                "Turning status requires desired rotation, signed alignment delta, positive error magnitude, and IsAligned false.",
                "status");
        }
    }
}
