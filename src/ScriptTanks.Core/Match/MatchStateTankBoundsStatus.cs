namespace ScriptTanks.Core.Match;

/// <summary>
/// Outcome label for applying arena outer-bounds correction to one tank's position
/// in a future <see cref="MatchStateTankBoundsPipeline"/> tick step.
/// </summary>
/// <remarks>
/// Pure enum for result models. Does not execute clamping, apply wall or obstacle
/// collision, resolve tank-vs-tank collision, resolve projectile hits, advance
/// <see cref="MatchState.CurrentTick"/>, log, replay, or integrate Godot.
/// </remarks>
public enum MatchStateTankBoundsStatus
{
    /// <summary>
    /// Tank is destroyed; bounds correction is skipped and position is unchanged.
    /// </summary>
    SkippedDestroyed = 0,

    /// <summary>
    /// Tank is alive and its hitbox circle is already fully inside legal arena bounds.
    /// </summary>
    InsideBounds = 1,

    /// <summary>
    /// Tank is alive and its position was corrected into legal arena bounds.
    /// </summary>
    Clamped = 2,
}
