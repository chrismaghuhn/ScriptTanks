using System;
using System.Collections.Generic;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Selects a <see cref="MatchSensorScanRequest"/> from a scheduled list when
/// the entry at a caller-provided index matches the current tick.
/// </summary>
/// <remarks>
/// This selector inspects at most one indexed
/// <see cref="MatchScheduledSensorScanRequest"/> and compares its tick to
/// <paramref name="currentTick"/>. Schedule validation and ordering are
/// expected to be handled elsewhere. This type does not advance
/// <paramref name="nextIndex"/>, validate list order, sort, or mutate the
/// schedule. It does not execute scans or integrate with AI/script runtimes,
/// match runners, replay, logging, diagnostics, or Godot.
/// </remarks>
public static class MatchScheduledSensorScanRequestSelector
{
    public static bool TrySelectForTick(
        IReadOnlyList<MatchScheduledSensorScanRequest> scheduledScanRequests,
        SimTick currentTick,
        int nextIndex,
        out MatchSensorScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(scheduledScanRequests);

        if (nextIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIndex),
                nextIndex,
                "Next scheduled sensor scan request index must not be negative.");
        }

        if (nextIndex >= scheduledScanRequests.Count)
        {
            request = default;
            return false;
        }

        MatchScheduledSensorScanRequest scheduled = scheduledScanRequests[nextIndex];

        if (scheduled.Tick.Value == currentTick.Value)
        {
            request = scheduled.Request;
            return true;
        }

        request = default;
        return false;
    }
}
