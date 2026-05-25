using System;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure deterministic static obstacle (wall) collision response for one tick snapshot.
/// Applies the MVP blocked-move / rollback policy using pre-movement and
/// post-movement/post-bounds candidate tank positions.
/// </summary>
/// <remarks>
/// Requires both <paramref name="preMovementState"/> and <paramref name="candidateState"/>
/// because rollback restores tanks to pre-movement positions when occupancy overlaps a
/// <see cref="ArenaDefinition.WallBlocks"/> entry. Uses
/// <see cref="TankWallOccupancyHelper"/> in arena wall-block order.
/// Does not perform movement integration, arena outer-bounds clamping, projectile resolution,
/// advance <see cref="MatchState.CurrentTick"/>, run <see cref="MatchTickPipeline"/>, log,
/// replay, integrate Godot, pathfinding, sliding, or swept collision.
/// Final tanks are candidate-owned; only <see cref="MovementState.Position"/> may change on rollback.
/// </remarks>
public static class MatchStateTankObstacleCollisionPipeline
{
    /// <summary>
    /// Applies wall occupancy collision for every tank in index order.
    /// </summary>
    /// <param name="preMovementState">State before movement integration; rollback position source.</param>
    /// <param name="candidateState">State after movement and bounds; candidate positions and arena walls.</param>
    /// <returns>
    /// Pre-movement state, candidate state, final state with corrected positions, and per-tank records.
    /// </returns>
    public static MatchStateTankObstacleCollisionResult Step(
        MatchState preMovementState,
        MatchState candidateState)
    {
        ArgumentNullException.ThrowIfNull(preMovementState);
        ArgumentNullException.ThrowIfNull(candidateState);

        ValidateTankParity(preMovementState, candidateState);

        ArenaDefinition arena = candidateState.Arena;
        TankState[] tanks = candidateState.Tanks.ToArray();
        MatchStateTankObstacleCollisionRecord[] records =
            new MatchStateTankObstacleCollisionRecord[candidateState.Tanks.Count];

        for (int i = 0; i < candidateState.Tanks.Count; i++)
        {
            TankState preMovementTank = preMovementState.Tanks[i];
            TankState candidateTank = candidateState.Tanks[i];
            FixedVec2 initialPosition = preMovementTank.Movement.Position;
            FixedVec2 candidatePosition = candidateTank.Movement.Position;

            if (candidateTank.IsDestroyed)
            {
                records[i] = new MatchStateTankObstacleCollisionRecord(
                    i,
                    candidateTank.Id,
                    MatchStateTankObstacleCollisionStatus.SkippedDestroyed,
                    initialPosition,
                    candidatePosition,
                    candidatePosition,
                    blockingWallBlockId: null);
                continue;
            }

            Fixed hitboxRadius = candidateTank.Definition.Stats.HitboxRadius;

            string? initialBlockingWallId = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
                arena,
                initialPosition,
                hitboxRadius);

            if (initialBlockingWallId is not null)
            {
                tanks[i] = RollbackPosition(candidateTank, initialPosition);

                records[i] = MatchStateTankObstacleCollisionRecord.StartedInsideObstacle(
                    i,
                    candidateTank.Id,
                    initialPosition,
                    candidatePosition,
                    initialBlockingWallId);
                continue;
            }

            string? candidateBlockingWallId = TankWallOccupancyHelper.FindFirstBlockingWallBlockId(
                arena,
                candidatePosition,
                hitboxRadius);

            if (candidateBlockingWallId is not null)
            {
                tanks[i] = RollbackPosition(candidateTank, initialPosition);

                records[i] = MatchStateTankObstacleCollisionRecord.BlockedByObstacle(
                    i,
                    candidateTank.Id,
                    initialPosition,
                    candidatePosition,
                    candidateBlockingWallId);
                continue;
            }

            records[i] = MatchStateTankObstacleCollisionRecord.Unchanged(
                i,
                candidateTank.Id,
                initialPosition,
                candidatePosition);
        }

        MatchState finalState = candidateState.WithTanks(tanks);

        return new MatchStateTankObstacleCollisionResult(
            preMovementState,
            candidateState,
            finalState,
            records);
    }

    private static void ValidateTankParity(MatchState preMovementState, MatchState candidateState)
    {
        if (candidateState.Tanks.Count != preMovementState.Tanks.Count)
        {
            throw new ArgumentException(
                "Candidate state tank count must match pre-movement state tank count.",
                nameof(candidateState));
        }

        for (int i = 0; i < candidateState.Tanks.Count; i++)
        {
            if (candidateState.Tanks[i].Id != preMovementState.Tanks[i].Id)
            {
                throw new ArgumentException(
                    "Candidate state tank id must match pre-movement state tank id at the same index.",
                    nameof(candidateState));
            }
        }
    }

    private static TankState RollbackPosition(TankState candidateTank, FixedVec2 preMovementPosition)
        => candidateTank.WithMovement(
            candidateTank.Movement.WithPosition(preMovementPosition));
}
