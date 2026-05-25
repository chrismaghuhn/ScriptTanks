namespace ScriptTanks.Core.Match;

/// <summary>
/// Outcome label for applying static obstacle (wall) collision response to one tank's
/// position in a future <see cref="MatchStateTankObstacleCollisionPipeline"/> tick step.
/// </summary>
/// <remarks>
/// Pure enum for result models. Does not execute collision checks, apply arena outer
/// bounds, resolve tank-vs-tank collision, resolve projectile hits, advance
/// <see cref="MatchState.CurrentTick"/>, log, replay, or integrate Godot.
/// </remarks>
public enum MatchStateTankObstacleCollisionStatus
{
    /// <summary>
    /// Bounded candidate position is legal; final position remains the candidate.
    /// </summary>
    Unchanged = 0,

    /// <summary>
    /// Bounded candidate overlaps a wall; final position is rolled back to pre-movement.
    /// </summary>
    BlockedByObstacle = 1,

    /// <summary>
    /// Tank is destroyed; obstacle collision is skipped and position follows the candidate.
    /// </summary>
    SkippedDestroyed = 2,

    /// <summary>
    /// Pre-movement position already overlaps a wall; MVP does not recover occupancy.
    /// </summary>
    StartedInsideObstacle = 3,
}
