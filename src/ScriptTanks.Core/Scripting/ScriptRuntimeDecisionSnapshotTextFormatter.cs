using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces a stable one-line debug representation of a <see cref="ScriptRuntimeDecisionSnapshot"/>.
/// </summary>
/// <remarks>
/// Includes <see cref="ScriptRuntimeDecisionSnapshot.Tick"/>, <see cref="ScriptRuntimeDecisionSnapshot.TankIndex"/>,
/// and the formatted <see cref="ScriptRuntimeDecisionTextFormatter"/> output for
/// <see cref="ScriptRuntimeDecisionSnapshot.Result"/>. This type does not log, integrate diagnostics
/// or UI, execute commands, generate match, sensor, weapon, or movement requests, mutate match
/// state, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeDecisionSnapshotTextFormatter
{
    public static string Format(
        ScriptRuntimeDecisionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string resultText = ScriptRuntimeDecisionTextFormatter.Format(
            snapshot.Result);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptRuntimeDecisionSnapshot {{ Tick = {snapshot.Tick}, TankIndex = {snapshot.TankIndex}, Result = {resultText} }}");
    }
}
