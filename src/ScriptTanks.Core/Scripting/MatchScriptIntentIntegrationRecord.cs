using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure record of one tank's integrated script evaluation: built evaluation context,
/// full runtime evaluation record, and translator v2 output.
/// </summary>
/// <remarks>
/// This type carries data only. It does not evaluate scripts, translate commands,
/// execute gameplay actions, dispatch requests, mutate match state, log, record replays,
/// or integrate Godot.
/// </remarks>
public sealed class MatchScriptIntentIntegrationRecord
{
    public int TankIndex { get; }

    public TankId TankId { get; }

    public ScriptEvaluationContext Context { get; }

    public ScriptRuntimeEvaluationRecord EvaluationRecord { get; }

    public ScriptCommandTranslationOutput TranslationOutput { get; }

    public MatchScriptIntentIntegrationRecord(
        int tankIndex,
        TankId tankId,
        ScriptEvaluationContext context,
        ScriptRuntimeEvaluationRecord evaluationRecord,
        ScriptCommandTranslationOutput translationOutput)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tankIndex), tankIndex, null);
        }

        ArgumentNullException.ThrowIfNull(evaluationRecord);
        ArgumentNullException.ThrowIfNull(translationOutput);

        TankIndex = tankIndex;
        TankId = tankId;
        Context = context;
        EvaluationRecord = evaluationRecord;
        TranslationOutput = translationOutput;
    }
}
