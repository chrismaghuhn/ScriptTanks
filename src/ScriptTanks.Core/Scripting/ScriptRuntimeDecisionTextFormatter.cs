using System;
using System.Globalization;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Produces a stable one-line debug representation of a <see cref="ScriptRuntimeDecisionResult"/>.
/// </summary>
/// <remarks>
/// Pure string formatting for inspection and future overlays; output is not a serialization contract.
/// This type does not log, integrate diagnostics or UI, execute commands, generate match, sensor,
/// weapon, or movement requests, mutate match state, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeDecisionTextFormatter
{
    public static string Format(
        ScriptRuntimeDecisionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"ScriptRuntimeDecisionResult {{ Decision.HasCommand = {result.Decision.HasCommand}, Decision.RoutineIndex = {result.Decision.RoutineIndex}, Intent.HasIntent = {result.Intent.HasIntent}, Intent.RoutineIndex = {result.Intent.RoutineIndex}, Translation.Status = {result.Translation.Status}, Translation.RoutineIndex = {result.Translation.RoutineIndex}, Translation.Command = {FormatCommand(result.Translation.Command)}, Translation.Message = {result.Translation.Message} }}");
    }

    private static string FormatCommand(ScriptCommand? command)
    {
        return command.HasValue
            ? command.Value.Type.ToString()
            : "None";
    }
}
