using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable bundle of script routine decision, command intent, and translation result stages for
/// future script runtime and debugging flows.
/// </summary>
/// <remarks>
/// Groups <see cref="ScriptRoutineDecision"/>, <see cref="ScriptCommandIntent"/>, and
/// <see cref="ScriptCommandTranslationResult"/> without validating consistency across stages.
/// This type does not execute commands, generate concrete match, sensor, weapon, or movement
/// requests, mutate match state, log, record replays, run diagnostics, or integrate Godot.
/// </remarks>
public sealed class ScriptRuntimeDecisionResult
{
    public ScriptRoutineDecision Decision { get; }

    public ScriptCommandIntent Intent { get; }

    public ScriptCommandTranslationResult Translation { get; }

    public ScriptRuntimeDecisionResult(
        ScriptRoutineDecision decision,
        ScriptCommandIntent intent,
        ScriptCommandTranslationResult translation)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(translation);

        Decision = decision;
        Intent = intent;
        Translation = translation;
    }
}
