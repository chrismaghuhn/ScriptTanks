using System.Collections.Generic;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure wrapper around <see cref="CombinedRuntimeTickPipeline.Step"/> that also
/// creates combat-log entries for one combined script-runtime tick.
/// </summary>
/// <remarks>
/// This type only composes existing pure systems. It does not mutate inputs,
/// emit logs globally, integrate with runners, replay recorders, serialization,
/// UI, or Godot. All exception semantics come from the wrapped delegate.
/// </remarks>
public static class LoggedCombinedRuntimeTickPipeline
{
    public static LoggedCombinedRuntimeTickResult Step(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<ScriptProgram> programs,
        ProjectileIdSequence projectileIdSequence)
    {
        CombinedRuntimeTickResult tickResult =
            CombinedRuntimeTickPipeline.Step(runtime, programs, projectileIdSequence);

        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        return new LoggedCombinedRuntimeTickResult(tickResult, log);
    }
}
