using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Deterministic combined-runtime match runner. Evaluates the initial end
/// condition, then repeatedly applies
/// <see cref="CombinedRuntimeTickPipeline.Step(MatchSensorRuntimeState, IReadOnlyList{ScriptProgram}, ProjectileIdSequence)"/>
/// and re-evaluates via
/// <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/>
/// until the match has ended.
/// </summary>
/// <remarks>
/// <para>
/// Composes only existing pure systems. The initial end check runs before the
/// first combined tick, so <c>maxTicks == 0</c> or an already-decided state
/// returns immediately with <see cref="MatchRunResult.TicksExecuted"/> equal to
/// zero, <see cref="CombinedRuntimeRunResult.HasLastTickResult"/> false, and
/// <see cref="CombinedRuntimeRunResult.FinalRuntime"/> referencing the input
/// runtime.
/// </para>
/// <para>
/// <paramref name="programs"/> validation is delegated to the first
/// <see cref="CombinedRuntimeTickPipeline.Step"/> call (via intent evaluation).
/// When the loop never runs, invalid programs are not checked.
/// </para>
/// <para>
/// Out of scope: replay recording, combat logs, logged runners, Godot
/// integration, and changes to <see cref="MatchRunner"/>.
/// </para>
/// </remarks>
public static class CombinedRuntimeRunner
{
    public static CombinedRuntimeRunResult RunUntilEnd(
        MatchSensorRuntimeState initialRuntime,
        IReadOnlyList<ScriptProgram> programs,
        ProjectileIdSequence initialProjectileIdSequence,
        int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(initialRuntime);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        MatchSensorRuntimeState runtime = initialRuntime;
        ProjectileIdSequence projectileSequence = initialProjectileIdSequence;
        int ticksExecuted = 0;
        CombinedRuntimeTickResult? lastTickResult = null;

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);

        while (!endCondition.IsEnded)
        {
            CombinedRuntimeTickResult tickResult =
                CombinedRuntimeTickPipeline.Step(runtime, programs, projectileSequence);

            runtime = tickResult.FinalRuntime;
            projectileSequence = tickResult.FinalProjectileIdSequence;
            lastTickResult = tickResult;
            ticksExecuted++;

            endCondition = MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            runtime.State,
            endCondition,
            ticksExecuted);

        return new CombinedRuntimeRunResult(
            runtime,
            runResult,
            projectileSequence,
            lastTickResult);
    }
}
