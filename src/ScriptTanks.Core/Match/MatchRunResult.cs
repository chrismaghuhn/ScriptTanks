using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable result returned by <see cref="MatchRunner.RunUntilEnd(MatchState, int)"/>.
/// Bundles the post-run match snapshot, the end-condition classification
/// that terminated the loop, and the number of <c>MatchTickPipeline.Step</c>
/// calls executed.
/// </summary>
/// <remarks>
/// <para>
/// This type is a plain data carrier. It does not run gameplay systems,
/// advance ticks, evaluate end conditions, mutate state, create combat logs,
/// create replay frames, determine teams, compute rankings, serialize state,
/// or interact with Godot.
/// </para>
/// <para>
/// The constructor validates that <see cref="FinalState"/> and
/// <see cref="EndCondition"/> are non-null and that
/// <see cref="TicksExecuted"/> is non-negative. Equality is intentionally
/// reference-based (no <c>Equals</c> override).
/// </para>
/// </remarks>
public sealed class MatchRunResult
{
    public MatchState FinalState { get; }

    public MatchEndConditionResult EndCondition { get; }

    public int TicksExecuted { get; }

    public MatchRunResult(
        MatchState finalState,
        MatchEndConditionResult endCondition,
        int ticksExecuted)
    {
        ArgumentNullException.ThrowIfNull(finalState);
        ArgumentNullException.ThrowIfNull(endCondition);

        if (ticksExecuted < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticksExecuted),
                ticksExecuted,
                "TicksExecuted must not be negative.");
        }

        FinalState = finalState;
        EndCondition = endCondition;
        TicksExecuted = ticksExecuted;
    }
}
