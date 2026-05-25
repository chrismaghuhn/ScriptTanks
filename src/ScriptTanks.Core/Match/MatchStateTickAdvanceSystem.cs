using System;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure <see cref="MatchState"/> system that advances only
/// <see cref="MatchState.CurrentTick"/> by exactly one tick via
/// <see cref="ScriptTanks.Core.Simulation.SimTick.Next"/>. Arena, tanks,
/// loadouts, and projectiles pass through unchanged.
/// <para>
/// The system adds no gameplay rules. It does NOT step projectiles, detect
/// or resolve hits, clean up projectiles, move tanks, resolve fire attempts,
/// mutate loadouts, evaluate match end-conditions, generate combat logs or
/// replay frames, serialize state, or interact with any Godot/scripting
/// layer. Higher-level pipelines compose this system with the projectile
/// and (future) tank/fire systems.
/// </para>
/// </summary>
public static class MatchStateTickAdvanceSystem
{
    public static MatchState AdvanceTick(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.WithCurrentTick(state.CurrentTick.Next());
    }
}
