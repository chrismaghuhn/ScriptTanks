using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Batch outcome of applying static obstacle (wall) collision for all tanks in one tick,
/// carrying pre-movement, bounded candidate, and final match states plus per-tank records.
/// </summary>
/// <remarks>
/// Defensively copies obstacle collision records and validates each record's positions
/// against <see cref="PreMovementState"/>, <see cref="CandidateState"/>, and
/// <see cref="FinalState"/>. Does not execute collision checks, run
/// <see cref="MatchTickPipeline"/>, resolve projectile hits, advance ticks, log, replay,
/// or integrate Godot.
/// </remarks>
public sealed class MatchStateTankObstacleCollisionResult
{
    private readonly MatchStateTankObstacleCollisionRecord[] _records;

    public MatchState PreMovementState { get; }

    public MatchState CandidateState { get; }

    public MatchState FinalState { get; }

    public IReadOnlyList<MatchStateTankObstacleCollisionRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public MatchStateTankObstacleCollisionResult(
        MatchState preMovementState,
        MatchState candidateState,
        MatchState finalState,
        IEnumerable<MatchStateTankObstacleCollisionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(preMovementState);
        ArgumentNullException.ThrowIfNull(candidateState);
        ArgumentNullException.ThrowIfNull(finalState);
        ArgumentNullException.ThrowIfNull(records);

        MatchStateTankObstacleCollisionRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        if (copy.Length != preMovementState.Tanks.Count)
        {
            throw new ArgumentException(
                "Obstacle collision record count must match pre-movement state tank count.",
                nameof(records));
        }

        if (candidateState.Tanks.Count != preMovementState.Tanks.Count)
        {
            throw new ArgumentException(
                "Candidate state tank count must match pre-movement state tank count.",
                nameof(candidateState));
        }

        if (finalState.Tanks.Count != preMovementState.Tanks.Count)
        {
            throw new ArgumentException(
                "Final state tank count must match pre-movement state tank count.",
                nameof(finalState));
        }

        for (int i = 0; i < copy.Length; i++)
        {
            MatchStateTankObstacleCollisionRecord record = copy[i];

            if (record.TankIndex != i)
            {
                throw new ArgumentException(
                    "Obstacle collision record tank index must match its position in the record sequence.",
                    nameof(records));
            }

            if (record.TankId != preMovementState.Tanks[i].Id)
            {
                throw new ArgumentException(
                    "Obstacle collision record tank id must match the tank at the same index in pre-movement state.",
                    nameof(records));
            }

            if (!record.InitialPosition.Equals(preMovementState.Tanks[i].Movement.Position))
            {
                throw new ArgumentException(
                    "Obstacle collision record initial position must match the tank movement position in pre-movement state.",
                    nameof(records));
            }

            if (!record.CandidatePosition.Equals(candidateState.Tanks[i].Movement.Position))
            {
                throw new ArgumentException(
                    "Obstacle collision record candidate position must match the tank movement position in candidate state.",
                    nameof(records));
            }

            if (!record.FinalPosition.Equals(finalState.Tanks[i].Movement.Position))
            {
                throw new ArgumentException(
                    "Obstacle collision record final position must match the tank movement position in final state.",
                    nameof(records));
            }
        }

        PreMovementState = preMovementState;
        CandidateState = candidateState;
        FinalState = finalState;
        _records = copy;
    }

    public MatchStateTankObstacleCollisionRecord GetRecordAtIndex(int index)
    {
        if (index < 0 || index >= _records.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Record index is outside the match state tank obstacle collision result range.");
        }

        return _records[index];
    }
}
