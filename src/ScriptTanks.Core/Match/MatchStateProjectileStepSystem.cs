using System;
using System.Linq;
using ScriptTanks.Core.Projectiles;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure <see cref="MatchState"/> system that advances every projectile in
/// <see cref="MatchState.Projectiles"/> by exactly one
/// <see cref="ProjectileMovement.Step(ProjectileState)"/>. Returns a new
/// <see cref="MatchState"/> via <see cref="MatchState.WithProjectiles"/> so
/// that all snapshot invariants (non-null arena, non-empty tank list,
/// matching loadout count) are re-enforced automatically.
/// <para>
/// The system intentionally does NOT advance
/// <see cref="MatchState.CurrentTick"/>, move tanks, resolve fire attempts,
/// detect projectile-vs-tank or projectile-vs-wall hits, apply damage,
/// remove inactive projectiles, generate combat logs or replay frames, or
/// integrate with any Godot/scripting layer. Single-tick projectile
/// kinematics remain owned by <see cref="ProjectileMovement"/> as the single
/// source of truth.
/// </para>
/// </summary>
public static class MatchStateProjectileStepSystem
{
    public static MatchState StepProjectiles(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ProjectileState[] updatedProjectiles = state.Projectiles
            .Select(ProjectileMovement.Step)
            .ToArray();

        return state.WithProjectiles(updatedProjectiles);
    }
}
