using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Replay;

/// <summary>
/// Pure replay recorder for script-aware combined runtime matches. Captures a
/// full-state <see cref="MatchReplayFrame"/> at frame 0 and after every
/// <see cref="CombinedRuntimeTickPipeline.Step"/> while mirroring the loop
/// shape of <see cref="CombinedRuntimeRunner.RunUntilEnd"/>.
/// </summary>
/// <remarks>
/// <para>
/// Replay frames store <see cref="MatchState"/> only (no sensor loadouts,
/// script diagnostics, or projectile-id sequence). Frame 0 is recorded before
/// the initial end check; if the match is already ended, the recording contains
/// exactly one frame and no tick step runs.
/// </para>
/// <para>
/// <paramref name="programs"/> validation is delegated to the first
/// <see cref="CombinedRuntimeTickPipeline.Step"/> when the loop enters.
/// </para>
/// <para>
/// Out of scope: replay schema extension, combat logs, logged recorders,
/// serialization, playback, randomness, system-clock access, and Godot.
/// </para>
/// </remarks>
public static class CombinedRuntimeReplayRecorder
{
    public static MatchRecordedRunResult RecordUntilEnd(
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

        List<MatchReplayFrame> frames = new()
        {
            new MatchReplayFrame(0, runtime.State),
        };

        MatchEndConditionResult endCondition =
            MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);

        while (!endCondition.IsEnded)
        {
            CombinedRuntimeTickResult tickResult =
                CombinedRuntimeTickPipeline.Step(runtime, programs, projectileSequence);

            runtime = tickResult.FinalRuntime;
            projectileSequence = tickResult.FinalProjectileIdSequence;
            ticksExecuted++;

            frames.Add(new MatchReplayFrame(ticksExecuted, runtime.State));

            endCondition = MatchEndConditionEvaluator.Evaluate(runtime.State, maxTicks);
        }

        MatchRunResult runResult = new MatchRunResult(
            runtime.State,
            endCondition,
            ticksExecuted);

        MatchReplayRecording recording = new MatchReplayRecording(frames);

        return new MatchRecordedRunResult(runResult, recording);
    }
}
