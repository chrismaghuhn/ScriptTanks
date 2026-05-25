using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable per-tank outcome for one tank obstacle (wall) collision step produced by
/// <see cref="MatchStateTankObstacleCollisionPipeline"/>.
/// </summary>
/// <remarks>
/// Pure data carrier. <see cref="InitialPosition"/> is the pre-movement legal position;
/// <see cref="CandidatePosition"/> is post-movement and post-bounds; <see cref="FinalPosition"/>
/// is the position after obstacle policy. Does not execute collision checks itself,
/// advance ticks, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchStateTankObstacleCollisionRecord
{
    public int TankIndex { get; }

    public TankId TankId { get; }

    public FixedVec2 InitialPosition { get; }

    public FixedVec2 CandidatePosition { get; }

    public FixedVec2 FinalPosition { get; }

    public MatchStateTankObstacleCollisionStatus Status { get; }

    public string? BlockingWallBlockId { get; }

    public bool DidBlockMovement =>
        Status == MatchStateTankObstacleCollisionStatus.BlockedByObstacle
        || Status == MatchStateTankObstacleCollisionStatus.StartedInsideObstacle;

    public MatchStateTankObstacleCollisionRecord(
        int tankIndex,
        TankId tankId,
        MatchStateTankObstacleCollisionStatus status,
        FixedVec2 initialPosition,
        FixedVec2 candidatePosition,
        FixedVec2 finalPosition,
        string? blockingWallBlockId)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        if (!Enum.IsDefined(typeof(MatchStateTankObstacleCollisionStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Tank obstacle collision status must be a defined enum value.");
        }

        ValidateBlockingWallBlockId(status, blockingWallBlockId);
        ValidatePositionInvariants(status, initialPosition, candidatePosition, finalPosition);

        TankIndex = tankIndex;
        TankId = tankId;
        Status = status;
        InitialPosition = initialPosition;
        CandidatePosition = candidatePosition;
        FinalPosition = finalPosition;
        BlockingWallBlockId = blockingWallBlockId;
    }

    public static MatchStateTankObstacleCollisionRecord Unchanged(
        int tankIndex,
        TankId tankId,
        FixedVec2 initialPosition,
        FixedVec2 candidatePosition)
    {
        return new MatchStateTankObstacleCollisionRecord(
            tankIndex,
            tankId,
            MatchStateTankObstacleCollisionStatus.Unchanged,
            initialPosition,
            candidatePosition,
            candidatePosition,
            blockingWallBlockId: null);
    }

    public static MatchStateTankObstacleCollisionRecord BlockedByObstacle(
        int tankIndex,
        TankId tankId,
        FixedVec2 initialPosition,
        FixedVec2 candidatePosition,
        string blockingWallBlockId)
    {
        return new MatchStateTankObstacleCollisionRecord(
            tankIndex,
            tankId,
            MatchStateTankObstacleCollisionStatus.BlockedByObstacle,
            initialPosition,
            candidatePosition,
            initialPosition,
            blockingWallBlockId);
    }

    public static MatchStateTankObstacleCollisionRecord SkippedDestroyed(
        int tankIndex,
        TankId tankId,
        FixedVec2 position)
    {
        return new MatchStateTankObstacleCollisionRecord(
            tankIndex,
            tankId,
            MatchStateTankObstacleCollisionStatus.SkippedDestroyed,
            position,
            position,
            position,
            blockingWallBlockId: null);
    }

    public static MatchStateTankObstacleCollisionRecord StartedInsideObstacle(
        int tankIndex,
        TankId tankId,
        FixedVec2 initialPosition,
        FixedVec2 candidatePosition,
        string blockingWallBlockId)
    {
        return new MatchStateTankObstacleCollisionRecord(
            tankIndex,
            tankId,
            MatchStateTankObstacleCollisionStatus.StartedInsideObstacle,
            initialPosition,
            candidatePosition,
            initialPosition,
            blockingWallBlockId);
    }

    private static void ValidateBlockingWallBlockId(
        MatchStateTankObstacleCollisionStatus status,
        string? blockingWallBlockId)
    {
        switch (status)
        {
            case MatchStateTankObstacleCollisionStatus.BlockedByObstacle:
            case MatchStateTankObstacleCollisionStatus.StartedInsideObstacle:
                if (string.IsNullOrWhiteSpace(blockingWallBlockId))
                {
                    throw new ArgumentException(
                        "Blocking wall block id is required for this obstacle collision status.",
                        nameof(blockingWallBlockId));
                }

                break;

            case MatchStateTankObstacleCollisionStatus.Unchanged:
            case MatchStateTankObstacleCollisionStatus.SkippedDestroyed:
                if (blockingWallBlockId is not null)
                {
                    throw new ArgumentException(
                        "Blocking wall block id must be null for this obstacle collision status.",
                        nameof(blockingWallBlockId));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Tank obstacle collision status must be a defined enum value.");
        }
    }

    private static void ValidatePositionInvariants(
        MatchStateTankObstacleCollisionStatus status,
        FixedVec2 initialPosition,
        FixedVec2 candidatePosition,
        FixedVec2 finalPosition)
    {
        switch (status)
        {
            case MatchStateTankObstacleCollisionStatus.Unchanged:
                if (!finalPosition.Equals(candidatePosition))
                {
                    throw new ArgumentException(
                        "Unchanged tank obstacle records must have final position equal to candidate position.",
                        nameof(finalPosition));
                }

                break;

            case MatchStateTankObstacleCollisionStatus.BlockedByObstacle:
            case MatchStateTankObstacleCollisionStatus.StartedInsideObstacle:
                if (!finalPosition.Equals(initialPosition))
                {
                    throw new ArgumentException(
                        "Blocked or started-inside-obstacle tank records must have final position equal to initial position.",
                        nameof(finalPosition));
                }

                break;

            case MatchStateTankObstacleCollisionStatus.SkippedDestroyed:
                if (!finalPosition.Equals(candidatePosition))
                {
                    throw new ArgumentException(
                        "Skipped destroyed tank obstacle records must have final position equal to candidate position.",
                        nameof(finalPosition));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Tank obstacle collision status must be a defined enum value.");
        }
    }
}
