using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Bundles the outputs of combined script-runtime composition, explicit tank
/// movement integration, arena outer-bounds correction, static obstacle collision,
/// and one projectile tick phase for a single simulation tick.
/// </summary>
/// <remarks>
/// Pure data carrier for <see cref="CombinedRuntimeTickPipeline"/>. Does not run script
/// pipelines, evaluate end conditions, log, replay, or integrate Godot.
/// <see cref="CombinedRuntimeTickPipeline"/> orchestrates tank movement, tank bounds, and
/// tank obstacle collision explicitly before
/// <see cref="MatchTickProjectilePipeline.StepProjectilesAndResolveHits"/> and
/// <see cref="MatchStateTickAdvanceSystem.AdvanceTick"/>.
/// <see cref="TankObstacleCollisionResult"/> is produced after bounds and before projectiles.
/// <see cref="SteppedState"/> is after projectile resolution and tick advance; it is not
/// the same reference as <see cref="MatchStateTankObstacleCollisionResult.FinalState"/>.
/// </remarks>
public sealed class CombinedRuntimeTickResult
{
    public MatchSensorRuntimeState InitialRuntime { get; }

    public CombinedScriptRuntimeComposerResult ScriptResult { get; }

    public MatchStateTankMovementResult TankMovementResult { get; }

    public MatchStateTankBoundsResult TankBoundsResult { get; }

    public MatchStateTankObstacleCollisionResult TankObstacleCollisionResult { get; }

    public MatchState SteppedState { get; }

    public MatchSensorRuntimeState FinalRuntime { get; }

    public ProjectileIdSequence FinalProjectileIdSequence { get; }

    public CombinedRuntimeTickResult(
        MatchSensorRuntimeState initialRuntime,
        CombinedScriptRuntimeComposerResult scriptResult,
        MatchStateTankMovementResult tankMovementResult,
        MatchStateTankBoundsResult tankBoundsResult,
        MatchStateTankObstacleCollisionResult tankObstacleCollisionResult,
        MatchState steppedState,
        MatchSensorRuntimeState finalRuntime,
        ProjectileIdSequence finalProjectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(initialRuntime);
        ArgumentNullException.ThrowIfNull(scriptResult);
        ArgumentNullException.ThrowIfNull(tankMovementResult);
        ArgumentNullException.ThrowIfNull(tankBoundsResult);
        ArgumentNullException.ThrowIfNull(tankObstacleCollisionResult);
        ArgumentNullException.ThrowIfNull(steppedState);
        ArgumentNullException.ThrowIfNull(finalRuntime);

        ValidateTickComposition(
            initialRuntime,
            scriptResult,
            tankMovementResult,
            tankBoundsResult,
            tankObstacleCollisionResult,
            steppedState,
            finalRuntime,
            finalProjectileIdSequence);

        InitialRuntime = initialRuntime;
        ScriptResult = scriptResult;
        TankMovementResult = tankMovementResult;
        TankBoundsResult = tankBoundsResult;
        TankObstacleCollisionResult = tankObstacleCollisionResult;
        SteppedState = steppedState;
        FinalRuntime = finalRuntime;
        FinalProjectileIdSequence = finalProjectileIdSequence;
    }

    private static void ValidateTickComposition(
        MatchSensorRuntimeState initialRuntime,
        CombinedScriptRuntimeComposerResult scriptResult,
        MatchStateTankMovementResult tankMovementResult,
        MatchStateTankBoundsResult tankBoundsResult,
        MatchStateTankObstacleCollisionResult tankObstacleCollisionResult,
        MatchState steppedState,
        MatchSensorRuntimeState finalRuntime,
        ProjectileIdSequence finalProjectileIdSequence)
    {
        if (!ReferenceEquals(initialRuntime, scriptResult.InitialRuntime))
        {
            throw new ArgumentException(
                "Initial runtime must be the same reference as scriptResult.InitialRuntime.",
                nameof(initialRuntime));
        }

        if (!ReferenceEquals(
                tankMovementResult.InitialState,
                scriptResult.FinalRuntime.State))
        {
            throw new ArgumentException(
                "Tank movement initial state must be the same reference as script result final runtime state.",
                nameof(tankMovementResult));
        }

        if (!ReferenceEquals(
                tankBoundsResult.InitialState,
                tankMovementResult.FinalState))
        {
            throw new ArgumentException(
                "Tank bounds initial state must be the same reference as tank movement final state.",
                nameof(tankBoundsResult));
        }

        if (!ReferenceEquals(
                tankObstacleCollisionResult.PreMovementState,
                tankMovementResult.InitialState))
        {
            throw new ArgumentException(
                "Tank obstacle collision pre-movement state must be the same reference as tank movement initial state.",
                nameof(tankObstacleCollisionResult));
        }

        if (!ReferenceEquals(
                tankObstacleCollisionResult.CandidateState,
                tankBoundsResult.FinalState))
        {
            throw new ArgumentException(
                "Tank obstacle collision candidate state must be the same reference as tank bounds final state.",
                nameof(tankObstacleCollisionResult));
        }

        if (!ReferenceEquals(finalRuntime.State, steppedState))
        {
            throw new ArgumentException(
                "Final runtime state must be the same reference as steppedState.",
                nameof(finalRuntime));
        }

        if (!ReferenceEquals(
                finalRuntime.SensorLoadouts,
                scriptResult.FinalRuntime.SensorLoadouts))
        {
            throw new ArgumentException(
                "Final runtime sensor loadouts must be the same reference as script result final sensor loadouts.",
                nameof(finalRuntime));
        }

        if (finalProjectileIdSequence != scriptResult.FinalProjectileIdSequence)
        {
            throw new ArgumentException(
                "Final projectile id sequence must match script result final projectile id sequence.",
                nameof(finalProjectileIdSequence));
        }
    }
}
