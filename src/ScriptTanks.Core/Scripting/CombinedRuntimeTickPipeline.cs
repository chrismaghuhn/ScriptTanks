using System;
using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure one-tick orchestration: combined script-runtime composition, tank movement
/// integration, arena bounds, static obstacle collision, projectile step/hit/cleanup,
/// tick advance, and folding the stepped <see cref="MatchState"/> back into the runtime snapshot.
/// </summary>
/// <remarks>
/// Fixed ordering: scripts → <see cref="MatchStateTankMovementPipeline"/> →
/// <see cref="MatchStateTankBoundsPipeline"/> →
/// <see cref="MatchStateTankObstacleCollisionPipeline"/> →
/// <see cref="MatchTickProjectilePipeline.StepProjectilesAndResolveHits"/> →
/// <see cref="MatchStateTickAdvanceSystem.AdvanceTick"/>.
/// Does not call <see cref="MatchTickPipeline.Step"/> so orchestration stays explicit.
/// Does not evaluate end conditions, run match loops, log, replay, schedule CPU work,
/// or integrate with Godot.
/// </remarks>
public static class CombinedRuntimeTickPipeline
{
    /// <summary>
    /// Runs script composition on <paramref name="runtime"/>, integrates tank movement,
    /// applies bounds and obstacle collision, resolves projectiles, advances
    /// <paramref name="runtime"/>.State by one tick, and returns all intermediate results
    /// plus the final runtime and projectile id sequence.
    /// </summary>
    public static CombinedRuntimeTickResult Step(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<ScriptProgram> programs,
        ProjectileIdSequence projectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        CombinedScriptRuntimeComposerResult scriptResult =
            CombinedScriptRuntimeComposer.Run(runtime, programs, projectileIdSequence);

        MatchStateTankMovementResult tankMovementResult =
            MatchStateTankMovementPipeline.Step(scriptResult.FinalRuntime.State);

        MatchStateTankBoundsResult tankBoundsResult =
            MatchStateTankBoundsPipeline.Step(tankMovementResult.FinalState);

        MatchStateTankObstacleCollisionResult tankObstacleCollisionResult =
            MatchStateTankObstacleCollisionPipeline.Step(
                tankMovementResult.InitialState,
                tankBoundsResult.FinalState);

        MatchState projectileResolvedState =
            MatchTickProjectilePipeline.StepProjectilesAndResolveHits(
                tankObstacleCollisionResult.FinalState);

        MatchState steppedState =
            MatchStateTickAdvanceSystem.AdvanceTick(projectileResolvedState);

        MatchSensorRuntimeState finalRuntime =
            scriptResult.FinalRuntime.WithState(steppedState);

        return new CombinedRuntimeTickResult(
            runtime,
            scriptResult,
            tankMovementResult,
            tankBoundsResult,
            tankObstacleCollisionResult,
            steppedState,
            finalRuntime,
            scriptResult.FinalProjectileIdSequence);
    }
}
