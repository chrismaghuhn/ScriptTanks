namespace ScriptTanks.Core.Match;

/// <summary>
/// Outcome label for integrating one tank's <see cref="Movement.MovementState"/>
/// in a future <see cref="MatchStateTankMovementPipeline"/> tick step.
/// </summary>
/// <remarks>
/// Pure enum for result models. Does not execute movement, apply arena bounds or
/// collision, resolve projectile hits, advance <see cref="MatchState.CurrentTick"/>,
/// log, replay, or integrate Godot.
/// </remarks>
public enum MatchStateTankMovementStatus
{
    /// <summary>
    /// Tank is destroyed; integration is skipped and movement is unchanged.
    /// </summary>
    SkippedDestroyed = 0,

    /// <summary>
    /// Tank is alive but <see cref="Movement.MovementState.VelocityPerTick"/> is zero;
    /// position is unchanged for this tick step.
    /// </summary>
    StayedStill = 1,

    /// <summary>
    /// Tank is alive and integration changed <see cref="Movement.MovementState.Position"/>.
    /// </summary>
    Moved = 2,
}
