using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable result adapter for a future script-aware combined runtime match run.
/// Bundles the final <see cref="MatchSensorRuntimeState"/>, a
/// <see cref="MatchRunResult"/>-compatible summary, the closing
/// <see cref="ProjectileIdSequence"/>, and optionally the last per-tick
/// <see cref="CombinedRuntimeTickResult"/>.
/// </summary>
/// <remarks>
/// Pure data carrier for a future <c>CombinedRuntimeRunner</c>. Does not run match
/// loops, evaluate end conditions, record replay frames, emit combat logs, or
/// integrate with Godot. Equality is intentionally reference-based (no
/// <c>Equals</c> override).
/// </remarks>
public sealed class CombinedRuntimeRunResult
{
    public MatchSensorRuntimeState FinalRuntime { get; }

    public MatchRunResult RunResult { get; }

    public ProjectileIdSequence FinalProjectileIdSequence { get; }

    public CombinedRuntimeTickResult? LastTickResult { get; }

    public bool HasLastTickResult => LastTickResult is not null;

    public CombinedRuntimeRunResult(
        MatchSensorRuntimeState finalRuntime,
        MatchRunResult runResult,
        ProjectileIdSequence finalProjectileIdSequence,
        CombinedRuntimeTickResult? lastTickResult)
    {
        ArgumentNullException.ThrowIfNull(finalRuntime);
        ArgumentNullException.ThrowIfNull(runResult);

        ValidateRunComposition(
            finalRuntime,
            runResult,
            finalProjectileIdSequence,
            lastTickResult);

        FinalRuntime = finalRuntime;
        RunResult = runResult;
        FinalProjectileIdSequence = finalProjectileIdSequence;
        LastTickResult = lastTickResult;
    }

    private static void ValidateRunComposition(
        MatchSensorRuntimeState finalRuntime,
        MatchRunResult runResult,
        ProjectileIdSequence finalProjectileIdSequence,
        CombinedRuntimeTickResult? lastTickResult)
    {
        if (!ReferenceEquals(finalRuntime.State, runResult.FinalState))
        {
            throw new ArgumentException(
                "Final runtime state must be the same reference as runResult.FinalState.",
                nameof(finalRuntime));
        }

        if (lastTickResult is null)
        {
            return;
        }

        if (!ReferenceEquals(finalRuntime, lastTickResult.FinalRuntime))
        {
            throw new ArgumentException(
                "Final runtime must be the same reference as lastTickResult.FinalRuntime.",
                nameof(finalRuntime));
        }

        if (finalProjectileIdSequence != lastTickResult.FinalProjectileIdSequence)
        {
            throw new ArgumentException(
                "Final projectile id sequence must match lastTickResult.FinalProjectileIdSequence.",
                nameof(finalProjectileIdSequence));
        }
    }
}
