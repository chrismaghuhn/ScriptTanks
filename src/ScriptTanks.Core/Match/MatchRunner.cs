using System;
using System.Collections.Generic;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Minimal deterministic match runner. Given an initial
/// <see cref="MatchState"/> and a <c>maxTicks</c> budget, it evaluates the
/// initial end condition, then repeatedly applies
/// <see cref="MatchTickPipeline.Step(MatchState)"/> and re-evaluates the
/// end condition via
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/>
/// until the match has ended.
/// </summary>
/// <remarks>
/// <para>
/// The runner composes only the existing pure systems and adds no new
/// gameplay rules. The initial end check happens before the first step, so
/// <c>maxTicks == 0</c> or an already-decided destruction state returns
/// immediately with <see cref="MatchRunResult.TicksExecuted"/> equal to
/// zero and <see cref="MatchRunResult.FinalState"/> referencing the input
/// state.
/// </para>
/// <para>
/// Termination is guaranteed: <c>maxTicks &lt; 0</c> is rejected,
/// <see cref="MatchTickPipeline.Step"/> advances <c>CurrentTick</c> by
/// exactly one, and
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/>
/// returns a timeout result once the tick budget is exhausted.
/// </para>
/// <para>
/// Out of scope: fire resolution, tank movement, AI/script execution,
/// new projectile rules, combat logs, replay frames, serialization, Godot
/// integration, ranking, rewards, randomness, and any system-clock access.
/// </para>
/// </remarks>
public static class MatchRunner
{
    public static MatchRunResult RunUntilEnd(MatchState initialState, int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(initialState);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        MatchState current = initialState;
        int ticksExecuted = 0;

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(current, maxTicks);

        while (!endCondition.IsEnded)
        {
            current = MatchTickPipeline.Step(current);
            ticksExecuted++;
            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        return new MatchRunResult(current, endCondition, ticksExecuted);
    }

    /// <summary>
    /// Deterministic match-run overload that consumes a strictly ordered
    /// list of <see cref="MatchScheduledFireRequest"/> values and applies
    /// at most one explicit fire request per tick before the projectile
    /// pipeline / tick advance, via
    /// <see cref="MatchTickFireThenProjectilePipeline.Step(MatchState, MatchFireRequest?)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Schedule shape rules (validated up front):
    /// <list type="bullet">
    ///   <item><description>
    ///     No <see cref="MatchScheduledFireRequest.Tick"/> may be earlier
    ///     than <see cref="MatchState.CurrentTick"/> of
    ///     <paramref name="initialState"/>.
    ///   </description></item>
    ///   <item><description>
    ///     Ticks must be strictly increasing (no duplicates, no decreases).
    ///   </description></item>
    /// </list>
    /// All shape violations raise <see cref="ArgumentException"/> with
    /// <see cref="ArgumentException.ParamName"/> equal to
    /// <c>"scheduledFireRequests"</c>.
    /// </para>
    /// <para>
    /// The initial end check runs before any scheduled request is
    /// consumed - so a tick-0 request is not applied if
    /// <paramref name="maxTicks"/> already terminates the match. Requests
    /// whose tick is not reached before the match ends are silently
    /// unused.
    /// </para>
    /// <para>
    /// Out of scope: AI / script execution, command queues, fire-intent
    /// generation, multiple requests per tick, projectile-id allocation,
    /// aiming, muzzle calculation, combat logs, replay frames,
    /// serialization, randomness, system-clock access, and Godot
    /// integration.
    /// </para>
    /// </remarks>
    public static MatchRunResult RunUntilEnd(
        MatchState initialState,
        int maxTicks,
        IReadOnlyList<MatchScheduledFireRequest> scheduledFireRequests)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(scheduledFireRequests);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        ValidateScheduledFireRequests(initialState, scheduledFireRequests);

        MatchState current = initialState;
        int ticksExecuted = 0;
        int nextRequestIndex = 0;

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(current, maxTicks);

        while (!endCondition.IsEnded)
        {
            MatchFireRequest? fireRequest = null;

            if (nextRequestIndex < scheduledFireRequests.Count
                && scheduledFireRequests[nextRequestIndex].Tick == current.CurrentTick)
            {
                fireRequest = scheduledFireRequests[nextRequestIndex].Request;
                nextRequestIndex++;
            }

            MatchTickFireThenProjectileOutcome tickOutcome =
                MatchTickFireThenProjectilePipeline.Step(current, fireRequest);

            current = tickOutcome.UpdatedState;
            ticksExecuted++;

            endCondition = MatchEndConditionEvaluator.Evaluate(current, maxTicks);
        }

        return new MatchRunResult(current, endCondition, ticksExecuted);
    }

    private static void ValidateScheduledFireRequests(
        MatchState initialState,
        IReadOnlyList<MatchScheduledFireRequest> scheduledFireRequests)
    {
        int previousTick = -1;

        for (int i = 0; i < scheduledFireRequests.Count; i++)
        {
            int tick = scheduledFireRequests[i].Tick.Value;

            if (tick < initialState.CurrentTick.Value)
            {
                throw new ArgumentException(
                    "Scheduled fire requests must not be earlier than the initial state's current tick.",
                    nameof(scheduledFireRequests));
            }

            if (i > 0 && tick <= previousTick)
            {
                throw new ArgumentException(
                    "Scheduled fire requests must be strictly ordered by tick and may not contain duplicate ticks.",
                    nameof(scheduledFireRequests));
            }

            previousTick = tick;
        }
    }
}
