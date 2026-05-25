using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure projectile pipeline composer. Applies the existing projectile
/// systems in a fixed deterministic order:
/// <list type="number">
///   <item><description><see cref="MatchStateProjectileStepSystem.StepProjectiles(MatchState)"/></description></item>
///   <item><description><see cref="MatchStateProjectileHitSystem.ResolveProjectileHits(MatchState)"/></description></item>
///   <item><description><see cref="MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(MatchState)"/></description></item>
/// </list>
/// <para>
/// The composer adds no new gameplay rules. It does NOT advance
/// <see cref="MatchState.CurrentTick"/>, fire weapons, mutate loadouts,
/// move tanks, generate combat logs or replay frames, raise death/winner
/// events, serialize state, or interact with any Godot/scripting layer.
/// Movement happens before hit detection so hits resolve against post-tick
/// projectile positions; cleanup runs last so projectiles deactivated by
/// hits or range depletion disappear within the same pipeline pass.
/// </para>
/// </summary>
public static class MatchTickProjectilePipeline
{
    public static MatchState StepProjectilesAndResolveHits(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        MatchState stepped = MatchStateProjectileStepSystem.StepProjectiles(state);
        MatchState resolved = MatchStateProjectileHitSystem.ResolveProjectileHits(stepped);
        MatchState cleaned = MatchStateProjectileCleanupSystem.RemoveInactiveProjectiles(resolved);

        return cleaned;
    }
}
