using System;
using System.Linq;
using ScriptTanks.Core.Projectiles;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure <see cref="MatchState"/> system that filters out inactive projectiles
/// from <see cref="MatchState.Projectiles"/>. The remaining
/// <see cref="ProjectileState"/> values stay in their original relative order
/// and are written back through <see cref="MatchState.WithProjectiles"/> so
/// that all snapshot invariants are re-enforced automatically.
/// <para>
/// The system is intentionally reason-agnostic: range depletion, wall hits,
/// tank hits, and manually deactivated projectiles are all treated the same
/// because the only signal it inspects is <see cref="ProjectileState.IsActive"/>.
/// </para>
/// <para>
/// The system does NOT advance <see cref="MatchState.CurrentTick"/>, move
/// projectiles, detect or resolve hits, apply damage, resolve fire attempts,
/// mutate tanks or loadouts, generate combat logs or replay frames, raise
/// death/winner events, serialize state, or interact with any
/// Godot/scripting layer.
/// </para>
/// </summary>
public static class MatchStateProjectileCleanupSystem
{
    public static MatchState RemoveInactiveProjectiles(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ProjectileState[] activeProjectiles = state.Projectiles
            .Where(projectile => projectile.IsActive)
            .ToArray();

        return state.WithProjectiles(activeProjectiles);
    }
}
