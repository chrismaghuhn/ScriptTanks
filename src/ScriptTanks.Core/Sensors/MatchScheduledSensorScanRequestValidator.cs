using System;
using System.Collections.Generic;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Validates the shape of an ordered list of
/// <see cref="MatchScheduledSensorScanRequest"/> values relative to an initial
/// simulation tick.
/// </summary>
/// <remarks>
/// Ensures no scheduled tick is earlier than <paramref name="initialTick"/> and
/// that ticks appear in strictly increasing order. This type does not sort,
/// queue, execute, or mutate requests. It does not validate against
/// <see cref="MatchSensorRuntimeState"/>, tank indices, or sensor slots. It does
/// not integrate AI/script runtimes, match runners, replay, logging,
/// diagnostics, or Godot.
/// </remarks>
public static class MatchScheduledSensorScanRequestValidator
{
    public static void Validate(
        SimTick initialTick,
        IReadOnlyList<MatchScheduledSensorScanRequest> scheduledScanRequests)
    {
        ArgumentNullException.ThrowIfNull(scheduledScanRequests);

        for (int i = 0; i < scheduledScanRequests.Count; i++)
        {
            MatchScheduledSensorScanRequest current = scheduledScanRequests[i];

            if (current.Tick.Value < initialTick.Value)
            {
                throw new ArgumentException(
                    "Scheduled sensor scan requests must not be earlier than the initial tick.",
                    nameof(scheduledScanRequests));
            }

            if (i > 0)
            {
                MatchScheduledSensorScanRequest previous = scheduledScanRequests[i - 1];

                if (current.Tick.Value <= previous.Tick.Value)
                {
                    throw new ArgumentException(
                        "Scheduled sensor scan requests must be strictly increasing by tick.",
                        nameof(scheduledScanRequests));
                }
            }
        }
    }
}
