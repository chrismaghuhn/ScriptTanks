using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Logged runners for the combined script-aware runtime match loop.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RunUntilEnd"/> emits only <c>match_started</c> and
/// <c>match_ended</c> by delegating simulation to
/// <see cref="CombinedRuntimeRunner.RunUntilEnd"/>.
/// </para>
/// <para>
/// <see cref="RunUntilEndWithTickLogs"/> runs the same loop as
/// <see cref="CombinedRuntimeRunner"/> but uses
/// <see cref="LoggedCombinedRuntimeTickPipeline.Step"/> per tick and merges
/// per-tick combat logs between start and end.
/// </para>
/// <para>
/// Does not modify <see cref="CombinedRuntimeRunner"/>, record replay frames,
/// integrate with Godot, or emit logs globally.
/// </para>
/// </remarks>
public static class LoggedCombinedRuntimeRunner
{
    public static LoggedCombinedRuntimeRunResult RunUntilEnd(
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

        CombatLog startLog = MatchCombatLogFactory.CreateMatchStartedLog(
            initialRuntime.State.CurrentTick);

        CombinedRuntimeRunResult runResult = CombinedRuntimeRunner.RunUntilEnd(
            initialRuntime,
            programs,
            initialProjectileIdSequence,
            maxTicks);

        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(
            runResult.FinalRuntime.State.CurrentTick,
            runResult.RunResult.EndCondition);

        CombatLog mergedLog = CombatLogMerger.Merge(startLog, endLog);

        return new LoggedCombinedRuntimeRunResult(runResult, mergedLog);
    }

    /// <summary>
    /// Runs the combined runtime match loop until an end condition is met and
    /// returns a run result with a combat log that includes per-tick script
    /// events from <see cref="LoggedCombinedRuntimeTickPipeline"/>.
    /// </summary>
    public static LoggedCombinedRuntimeRunResult RunUntilEndWithTickLogs(
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

        CombatLog startLog = MatchCombatLogFactory.CreateMatchStartedLog(
            initialRuntime.State.CurrentTick);

        MatchSensorRuntimeState runtime = initialRuntime;
        ProjectileIdSequence projectileSequence = initialProjectileIdSequence;
        int ticksExecuted = 0;
        CombinedRuntimeTickResult? lastTickResult = null;
        var tickLogs = new List<CombatLog>();

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);

        while (!endCondition.IsEnded)
        {
            LoggedCombinedRuntimeTickResult loggedTick =
                LoggedCombinedRuntimeTickPipeline.Step(runtime, programs, projectileSequence);

            runtime = loggedTick.TickResult.FinalRuntime;
            projectileSequence = loggedTick.TickResult.FinalProjectileIdSequence;
            lastTickResult = loggedTick.TickResult;
            ticksExecuted++;
            tickLogs.Add(loggedTick.Log);

            endCondition = MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            runtime.State,
            endCondition,
            ticksExecuted);

        CombinedRuntimeRunResult combinedRunResult = new CombinedRuntimeRunResult(
            runtime,
            runResult,
            projectileSequence,
            lastTickResult);

        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(
            combinedRunResult.FinalRuntime.State.CurrentTick,
            combinedRunResult.RunResult.EndCondition);

        var logs = new List<CombatLog> { startLog };
        logs.AddRange(tickLogs);
        logs.Add(endLog);
        CombatLog mergedLog = CombatLogMerger.Merge(logs);

        return new LoggedCombinedRuntimeRunResult(combinedRunResult, mergedLog);
    }
}
