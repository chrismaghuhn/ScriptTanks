using System;

using System.Collections.Generic;

using ScriptTanks.Core.Ids;

using ScriptTanks.Core.Logging;

using ScriptTanks.Core.Match;

using ScriptTanks.Core.Scripting;

using ScriptTanks.Core.Sensors;



namespace ScriptTanks.Core.Replay;



/// <summary>

/// Logged replay recorders for the combined script-aware runtime match loop.

/// </summary>

/// <remarks>

/// <para>

/// <see cref="RecordUntilEnd"/> emits only <c>match_started</c> and

/// <c>match_ended</c> by delegating recording to

/// <see cref="CombinedRuntimeReplayRecorder.RecordUntilEnd"/>.

/// </para>

/// <para>

/// <see cref="RecordUntilEndWithTickLogs"/> runs the same loop as

/// <see cref="CombinedRuntimeReplayRecorder"/> but uses

/// <see cref="LoggedCombinedRuntimeTickPipeline.Step"/> per tick, keeps

/// <see cref="MatchReplayFrame"/> with <see cref="MatchState"/> only, and

/// merges per-tick combat logs between start and end.

/// </para>

/// <para>

/// Does not modify <see cref="CombinedRuntimeReplayRecorder"/>, extend the

/// replay schema, integrate with Godot, or emit logs globally.

/// </para>

/// </remarks>

public static class LoggedCombinedRuntimeReplayRecorder

{

    public static LoggedCombinedRuntimeRecordedRunResult RecordUntilEnd(

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



        MatchRecordedRunResult recorded = CombinedRuntimeReplayRecorder.RecordUntilEnd(

            initialRuntime,

            programs,

            initialProjectileIdSequence,

            maxTicks);



        CombatLog startLog = MatchCombatLogFactory.CreateMatchStartedLog(

            initialRuntime.State.CurrentTick);



        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(

            recorded.RunResult.FinalState.CurrentTick,

            recorded.RunResult.EndCondition);



        CombatLog mergedLog = CombatLogMerger.Merge(startLog, endLog);



        return new LoggedCombinedRuntimeRecordedRunResult(recorded, mergedLog);

    }



    /// <summary>

    /// Records the combined runtime match loop until an end condition is met and

    /// returns a recorded run result with a combat log that includes per-tick

    /// script events from <see cref="LoggedCombinedRuntimeTickPipeline"/>.

    /// </summary>

    public static LoggedCombinedRuntimeRecordedRunResult RecordUntilEndWithTickLogs(

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

        var tickLogs = new List<CombatLog>();



        List<MatchReplayFrame> frames = new()

        {

            new MatchReplayFrame(0, runtime.State),

        };



        MatchEndConditionResult endCondition =

            MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);



        while (!endCondition.IsEnded)

        {

            LoggedCombinedRuntimeTickResult loggedTick =

                LoggedCombinedRuntimeTickPipeline.Step(runtime, programs, projectileSequence);



            runtime = loggedTick.TickResult.FinalRuntime;

            projectileSequence = loggedTick.TickResult.FinalProjectileIdSequence;

            ticksExecuted++;



            frames.Add(new MatchReplayFrame(ticksExecuted, runtime.State));

            tickLogs.Add(loggedTick.Log);



            endCondition = MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);

        }



        MatchRunResult runResult = new MatchRunResult(

            runtime.State,

            endCondition,

            ticksExecuted);



        MatchReplayRecording recording = new MatchReplayRecording(frames);

        MatchRecordedRunResult recorded = new MatchRecordedRunResult(runResult, recording);



        CombatLog endLog = MatchCombatLogFactory.CreateMatchEndedLog(

            runResult.FinalState.CurrentTick,

            runResult.EndCondition);



        var logs = new List<CombatLog> { startLog };

        logs.AddRange(tickLogs);

        logs.Add(endLog);

        CombatLog mergedLog = CombatLogMerger.Merge(logs);



        return new LoggedCombinedRuntimeRecordedRunResult(recorded, mergedLog);

    }

}


