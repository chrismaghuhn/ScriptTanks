using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure one-tick <see cref="MatchState"/> composer that chains the currently
/// implemented match-level systems in a fixed deterministic order:
/// <list type="number">
///   <item><description><see cref="MatchTickProjectilePipeline.StepProjectilesAndResolveHits(MatchState)"/> (projectile Step → Hit → Cleanup)</description></item>
///   <item><description><see cref="MatchStateTickAdvanceSystem.AdvanceTick(MatchState)"/> (CurrentTick += 1)</description></item>
/// </list>
/// <para>
/// The projectile pipeline runs before the tick advance so all movement, hit
/// resolution, and cleanup are applied to the current-tick snapshot before
/// <see cref="MatchState.CurrentTick"/> is incremented. This keeps future
/// log/replay events attributable to the tick they occurred in.
/// </para>
/// <para>
/// This composer is NOT a match runner. It does not implement a match loop,
/// max-tick or timeout checks, win/loss or end-condition logic, fire
/// resolution, tank movement, loadout mutation, AI/script execution,
/// combat logs, replay frames, serialization, or Godot integration.
/// </para>
/// </summary>
public static class MatchTickPipeline
{
    public static MatchState Step(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        MatchState afterProjectiles = MatchTickProjectilePipeline.StepProjectilesAndResolveHits(state);
        MatchState advanced = MatchStateTickAdvanceSystem.AdvanceTick(afterProjectiles);

        return advanced;
    }
}
