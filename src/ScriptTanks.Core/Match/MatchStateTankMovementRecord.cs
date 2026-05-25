using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Movement;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable per-tank outcome for one tank movement integration step produced by
/// <see cref="MatchStateTankMovementPipeline"/>.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not call <see cref="MovementIntegrator"/> itself, resolve projectile
/// hits, advance ticks, apply arena bounds or collision, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchStateTankMovementRecord
{
    public int TankIndex { get; }

    public TankId TankId { get; }

    public MatchStateTankMovementStatus Status { get; }

    public MovementState InitialMovement { get; }

    public MovementState FinalMovement { get; }

    public bool DidMove =>
        Status == MatchStateTankMovementStatus.Moved;

    public MatchStateTankMovementRecord(
        int tankIndex,
        TankId tankId,
        MatchStateTankMovementStatus status,
        MovementState initialMovement,
        MovementState finalMovement)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        if (!Enum.IsDefined(typeof(MatchStateTankMovementStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Tank movement status must be a defined enum value.");
        }

        ValidateMovementInvariants(status, initialMovement, finalMovement);

        TankIndex = tankIndex;
        TankId = tankId;
        Status = status;
        InitialMovement = initialMovement;
        FinalMovement = finalMovement;
    }

    public static MatchStateTankMovementRecord SkippedDestroyed(
        int tankIndex,
        TankId tankId,
        MovementState movement)
    {
        return new MatchStateTankMovementRecord(
            tankIndex,
            tankId,
            MatchStateTankMovementStatus.SkippedDestroyed,
            movement,
            movement);
    }

    public static MatchStateTankMovementRecord StayedStill(
        int tankIndex,
        TankId tankId,
        MovementState movement)
    {
        return new MatchStateTankMovementRecord(
            tankIndex,
            tankId,
            MatchStateTankMovementStatus.StayedStill,
            movement,
            movement);
    }

    public static MatchStateTankMovementRecord Moved(
        int tankIndex,
        TankId tankId,
        MovementState initialMovement,
        MovementState finalMovement)
    {
        return new MatchStateTankMovementRecord(
            tankIndex,
            tankId,
            MatchStateTankMovementStatus.Moved,
            initialMovement,
            finalMovement);
    }

    private static void ValidateMovementInvariants(
        MatchStateTankMovementStatus status,
        MovementState initialMovement,
        MovementState finalMovement)
    {
        switch (status)
        {
            case MatchStateTankMovementStatus.SkippedDestroyed:
            case MatchStateTankMovementStatus.StayedStill:
                if (!initialMovement.Equals(finalMovement))
                {
                    throw new ArgumentException(
                        "Skipped or stationary tank movement records must have equal initial and final movement.",
                        nameof(finalMovement));
                }

                break;

            case MatchStateTankMovementStatus.Moved:
                if (initialMovement.Equals(finalMovement))
                {
                    throw new ArgumentException(
                        "Moved tank movement records must have different initial and final movement.",
                        nameof(finalMovement));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Tank movement status must be a defined enum value.");
        }
    }
}
