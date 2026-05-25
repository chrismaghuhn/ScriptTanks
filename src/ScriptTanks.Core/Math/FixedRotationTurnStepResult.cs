using System;

namespace ScriptTanks.Core.Math;

/// <summary>
/// Pure result for one deterministic fixed-rotation turn step toward a desired rotation.
/// </summary>
/// <remarks>
/// Immutable data carrier for normalized current/desired/final rotations, the signed turn
/// applied this step, and alignment status. Does not integrate scripting pipelines, combat,
/// replay, logging, or UI behavior.
/// </remarks>
public sealed class FixedRotationTurnStepResult
{
    public Fixed CurrentRotation { get; }

    public Fixed DesiredRotation { get; }

    public Fixed FinalRotation { get; }

    public Fixed AppliedTurnDelta { get; }

    public Fixed AppliedTurnMagnitude { get; }

    public bool IsAligned { get; }

    public FixedRotationTurnStepResult(
        Fixed currentRotation,
        Fixed desiredRotation,
        Fixed finalRotation,
        Fixed appliedTurnDelta,
        Fixed appliedTurnMagnitude,
        bool isAligned)
    {
        if (appliedTurnMagnitude < Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(appliedTurnMagnitude),
                appliedTurnMagnitude,
                "Applied turn magnitude must not be negative.");
        }

        CurrentRotation = currentRotation;
        DesiredRotation = desiredRotation;
        FinalRotation = finalRotation;
        AppliedTurnDelta = appliedTurnDelta;
        AppliedTurnMagnitude = appliedTurnMagnitude;
        IsAligned = isAligned;
    }
}
