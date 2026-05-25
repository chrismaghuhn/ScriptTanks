using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces a stable one-line debug representation of a <see cref="ScriptRuntimeEvaluationRecord"/>.
/// </summary>
/// <remarks>
/// Includes tick, tank index, program routine count, evaluation context, and the formatted
/// <see cref="ScriptRuntimeDecisionResult"/> via <see cref="ScriptRuntimeDecisionTextFormatter"/>.
/// This type does not log, integrate diagnostics or UI, execute commands, generate match, sensor,
/// weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeEvaluationRecordTextFormatter
{
    public static string Format(
        ScriptRuntimeEvaluationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        string resultText = ScriptRuntimeDecisionTextFormatter.Format(
            record.Result);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptRuntimeEvaluationRecord {{ Tick = {record.Tick}, TankIndex = {record.TankIndex}, Program.Count = {record.Program.Count}, Context = {record.Context}, Result = {resultText} }}");
    }
}
