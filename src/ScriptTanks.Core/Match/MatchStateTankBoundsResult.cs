using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Batch outcome of applying arena outer-bounds correction for all tanks in one
/// <see cref="MatchState"/> snapshot in a future tank bounds tick pipeline.
/// </summary>
/// <remarks>
/// Defensively copies bounds records and validates each record's positions against
/// the initial and final match states. Does not execute clamping, run
/// <see cref="MatchTickPipeline"/>, resolve projectile hits, advance ticks, apply wall
/// or obstacle collision, log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchStateTankBoundsResult
{
    private readonly MatchStateTankBoundsRecord[] _records;

    public MatchState InitialState { get; }

    public MatchState FinalState { get; }

    public IReadOnlyList<MatchStateTankBoundsRecord> Records =>
        Array.AsReadOnly(_records);

    public int Count => _records.Length;

    public MatchStateTankBoundsResult(
        MatchState initialState,
        MatchState finalState,
        IEnumerable<MatchStateTankBoundsRecord> records)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(finalState);
        ArgumentNullException.ThrowIfNull(records);

        MatchStateTankBoundsRecord[] copy = records.ToArray();

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
                "Bounds record count must match initial state tank count.",
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
            MatchStateTankBoundsRecord record = copy[i];

            if (record.TankIndex != i)
            {
                throw new ArgumentException(
                    "Bounds record tank index must match its position in the record sequence.",
                    nameof(records));
            }

            if (record.TankId != initialState.Tanks[i].Id)
            {
                throw new ArgumentException(
                    "Bounds record tank id must match the tank at the same index in initial state.",
                    nameof(records));
            }

            if (!record.InitialPosition.Equals(initialState.Tanks[i].Movement.Position))
            {
                throw new ArgumentException(
                    "Bounds record initial position must match the tank movement position in initial state.",
                    nameof(records));
            }

            if (!record.FinalPosition.Equals(finalState.Tanks[i].Movement.Position))
            {
                throw new ArgumentException(
                    "Bounds record final position must match the tank movement position in final state.",
                    nameof(records));
            }
        }

        InitialState = initialState;
        FinalState = finalState;
        _records = copy;
    }

    public MatchStateTankBoundsRecord GetRecordAtIndex(int recordIndex)
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
                "Record index is outside the match state tank bounds result range.");
        }
    }
}
