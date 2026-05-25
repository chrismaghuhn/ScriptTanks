using System;
using System.Collections.Generic;
using ScriptTanks.Core.Sensors;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure match-level composer that pairs tanks with script programs by index, derives
/// evaluation contexts, runs the script evaluation pipeline, and translates intents via v2.
/// </summary>
/// <remarks>
/// This type does not execute gameplay commands, spawn projectiles, fire weapons, move tanks,
/// run sensor scans, dispatch translated requests, mutate <see cref="Match.MatchState"/> or
/// sensor runtime state, schedule CPU work, integrate combat logs or replay recording,
/// run diagnostics, or integrate Godot.
/// </remarks>
public static class MatchScriptIntentIntegrationComposer
{
    /// <summary>
    /// Evaluates script programs for every tank in index order and returns integration records
    /// plus the unchanged runtime snapshot reference.
    /// </summary>
    /// <param name="runtime">Sensor runtime snapshot; must not be null.</param>
    /// <param name="programs">
    /// One program per tank, aligned with <see cref="Match.MatchState.Tanks"/> indices.
    /// </param>
    /// <returns>
    /// Result bundling <paramref name="runtime"/> and defensive copies of per-tank records.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="runtime"/> or <paramref name="programs"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="programs"/> count does not match tank count or contains null.
    /// </exception>
    public static MatchScriptIntentIntegrationResult EvaluateIntents(
        MatchSensorRuntimeState runtime,
        IReadOnlyList<ScriptProgram> programs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(programs);

        int tankCount = runtime.State.Tanks.Count;

        if (programs.Count != tankCount)
        {
            throw new ArgumentException(
                "Program count must match tank count.",
                nameof(programs));
        }

        for (int i = 0; i < programs.Count; i++)
        {
            if (programs[i] is null)
            {
                throw new ArgumentException(
                    "Programs collection must not contain null elements.",
                    nameof(programs));
            }
        }

        MatchScriptIntentIntegrationRecord[] records =
            new MatchScriptIntentIntegrationRecord[tankCount];

        for (int tankIndex = 0; tankIndex < tankCount; tankIndex++)
        {
            ScriptEvaluationContext context =
                ScriptRuntimeContextBuilder.Build(runtime, tankIndex);

            ScriptRuntimeEvaluationRecord evaluationRecord =
                ScriptRuntimeEvaluationPipeline.Evaluate(
                    runtime.State.CurrentTick,
                    tankIndex,
                    programs[tankIndex],
                    context);

            ScriptCommandTranslationOutput translationOutput =
                ScriptCommandTranslatorV2.Translate(evaluationRecord.Result.Intent);

            records[tankIndex] = new MatchScriptIntentIntegrationRecord(
                tankIndex,
                runtime.State.Tanks[tankIndex].Id,
                context,
                evaluationRecord,
                translationOutput);
        }

        return new MatchScriptIntentIntegrationResult(runtime, records);
    }
}
