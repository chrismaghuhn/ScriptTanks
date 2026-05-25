using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Batch outcome of integrating tank movement for all tanks in one
/// <see cref="MatchState"/> snapshot in a future tank movement tick pipeline.
/// </summary>
/// <remarks>
/// Defensively copies movement records. Does not call <see cref="Movement.MovementIntegrator"/>,
/// run <see cref="MatchTickPipeline"/>, resolve projectile hits, advance ticks, apply arena
/// bounds or collision, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchStateTankMovementResult
{
    private readonly MatchStateTankMovementRecord[] _records;

    public MatchState InitialState { get; }

    public MatchState FinalState { get; }

    public IReadOnlyList<MatchStateTankMovementRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public MatchStateTankMovementResult(
        MatchState initialState,
        MatchState finalState,
        IEnumerable<MatchStateTankMovementRecord> records)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(finalState);
        ArgumentNullException.ThrowIfNull(records);

        MatchStateTankMovementRecord[] copy = records.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Record sequence must not contain null elements.",
                    nameof(records));
            }
        }

        if (copy.Length != initialState.Tanks.Count)
        {
            throw new ArgumentException(
                "Movement record count must match initial state tank count.",
                nameof(records));
        }

        if (finalState.Tanks.Count != initialState.Tanks.Count)
        {
            throw new ArgumentException(
                "Final state tank count must match initial state tank count.",
                nameof(finalState));
        }

        for (int i = 0; i < copy.Length; i++)
        {
            MatchStateTankMovementRecord record = copy[i];

            if (record.TankIndex != i)
            {
                throw new ArgumentException(
                    "Movement record tank index must match its position in the record sequence.",
                    nameof(records));
            }

            if (record.TankId != initialState.Tanks[i].Id)
            {
                throw new ArgumentException(
                    "Movement record tank id must match the tank at the same index in initial state.",
                    nameof(records));
            }
        }

        InitialState = initialState;
        FinalState = finalState;
        _records = copy;
    }

    public MatchStateTankMovementRecord GetRecordAtIndex(int recordIndex)
    {
        ValidateIndex(recordIndex);

        return _records[recordIndex];
    }

    private void ValidateIndex(int recordIndex)
    {
        if (recordIndex < 0 || recordIndex >= _records.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Record index is outside the match state tank movement result range.");
        }
    }
}
